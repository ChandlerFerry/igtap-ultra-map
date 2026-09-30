import argparse
import collections
import hashlib
import io
import json
import math
import multiprocessing
import os
import pickle
import sys
import time

import numpy as np
import UnityPy
from PIL import Image
from UnityPy.classes import PPtr
from UnityPy.export import SpriteHelper

TILE = 512
LEVELS = 10
BIG = 4 << 20
SKIP_COMPONENTS = {"Canvas", "RectTransform", "ParticleSystem"}
GAMEPLAY = {
    "SpringScript",
    "JiggleDropScript",
    "upgradeBox",
    "startGate",
    "endGate",
    "checkpointScript",
    "TeleporterScript",
    "PlatformMover",
    "ZipMover",
    "spikeScript",
}
DECOR_NAMES = ("decor", "light", "glowy", "plant", "wire", "cable", "vine", "leaf", "leaves", "flower", "grass", "bush")
SKIP_SCRIPTS = {"Movement", "ParallaxController", "backgroundScroller", "clonesScript", "PlayerSpriteCopier"}


Piece = collections.namedtuple("Piece", "order z state layer sprite tint x y w h fx fy rot fill")


class Painter:

    def __init__(self, sprites, scale, tile):
        self.sprites, self.scale, self.tile = sprites, scale, tile
        self.decoded, self.scaled = {}, {}

    def sprite(self, k):
        if k not in self.decoded:
            raw = self.sprites[k]
            self.decoded[k] = (Image.frombytes("RGBA", raw[1], raw[0]),) + raw[2:]
        return self.decoded[k]

    def tinted(self, k, tint):
        key = (k, tint)
        if key not in self.scaled:
            im = self.sprite(k)[0]
            if tint != (1, 1, 1, 1):
                a = np.asarray(im).astype(np.float32) * np.array(tint, dtype=np.float32)
                im = Image.fromarray(a.clip(0, 255).astype(np.uint8))
            self.scaled[key] = im
        return self.scaled[key]

    def flip(self, k, tint, fx, fy):
        im = self.tinted(k, tint)
        if fx:
            im = im.transpose(Image.FLIP_LEFT_RIGHT)
        if fy:
            im = im.transpose(Image.FLIP_TOP_BOTTOM)
        return im

    def cell(self, k, tint, fx, fy):
        key = (k, tint, fx, fy, "cell")
        if key not in self.scaled:
            _, ppu, rw, rh, _, _ = self.sprite(k)
            self.scaled[key] = resize(
                self.flip(k, tint, fx, fy), (max(1, round(rw / ppu * self.scale)), max(1, round(rh / ppu * self.scale)))
            )
        return self.scaled[key]

    def image(self, p):
        W, H = max(1, round(p.w * self.scale)), max(1, round(p.h * self.scale))
        key = (p.sprite, p.tint, W, H, p.fx, p.fy, p.rot, p.fill)
        if key not in self.scaled:
            if p.fill == "tile":
                c = self.cell(p.sprite, p.tint, p.fx, p.fy)
                im = Image.new("RGBA", (W, H))
                for x in range(0, W, c.width):
                    for y in range(0, H, c.height):
                        im.paste(c, (x, y))
            elif p.fill == "slice":
                _, ppu, _, _, _, border = self.sprite(p.sprite)
                im = slice9(
                    self.flip(p.sprite, p.tint, p.fx, p.fy),
                    border,
                    tuple(round(b / ppu * self.scale) for b in border),
                    W,
                    H,
                )
            else:
                im = resize(self.flip(p.sprite, p.tint, p.fx, p.fy), (W, H))
            if p.rot:
                im = im.rotate(p.rot, expand=True, resample=Image.BILINEAR)
            self.scaled[key] = im
        return self.scaled[key]

    def draw(self, dst, p, left, top):
        _, _, _, _, pv, _ = self.sprite(p.sprite)
        W, H = max(1, round(p.w * self.scale)), max(1, round(p.h * self.scale))
        x, y = (p.x - left) * self.scale - pv[0] * W, (top - p.y) * self.scale - (1 - pv[1]) * H
        if p.fill == "tile" and p.rot == 0 and W * H > BIG:
            c = self.cell(p.sprite, p.tint, p.fx, p.fy)
            tw, th = c.size
            x0, y0 = max(0, round(x)), max(0, round(y))
            x1, y1 = min(self.tile, round(x) + W), min(self.tile, round(y) + H)
            if x1 > x0 and y1 > y0:
                w, h = x1 - x0, y1 - y0
                ox, oy = (x0 - round(x)) % tw, (y0 - round(y)) % th
                arr = np.tile(np.asarray(c), (math.ceil((h + oy) / th), math.ceil((w + ox) / tw), 1))
                paste(dst, Image.fromarray(arr[oy : oy + h, ox : ox + w]), x0, y0)
            return
        im = self.image(p)
        dx, dy = (0.5 - pv[0]) * W, (pv[1] - 0.5) * H
        if p.fx:
            dx = -dx
        if p.fy:
            dy = -dy
        if p.rot:
            a = math.radians(p.rot)
            dx, dy = dx * math.cos(a) + dy * math.sin(a), -dx * math.sin(a) + dy * math.cos(a)
        cx, cy = x + pv[0] * W + dx, y + (1 - pv[1]) * H + dy
        paste(dst, im, round(cx - im.width / 2), round(cy - im.height / 2))


_work = {}


def _worker_init(payload, scale, quality, tile):
    with open(payload, "rb") as f:
        _work["painter"] = Painter(pickle.load(f), scale, tile)
    _work["quality"] = quality


def webp(img, quality):
    if img.getbbox() is None:
        return None
    buf = io.BytesIO()
    img.save(buf, "WEBP", quality=quality, method=4)
    return buf.getvalue()


def _tile_task(task):
    i, j, span, rows = task
    img = Image.new("RGBA", (TILE, TILE), (0, 0, 0, 0))
    left, top = i * span, (j + 1) * span
    for row in rows:
        _work["painter"].draw(img, Piece(*row), left, top)
    return i, j, webp(img, _work["quality"])


def _pyramid_task(task):
    i, j, children = task
    img = Image.new("RGBA", (2 * TILE, 2 * TILE), (0, 0, 0, 0))
    for di, dj, data in children:
        img.paste(Image.open(io.BytesIO(data)).convert("RGBA"), (di * TILE, (1 - dj) * TILE))
    return i, j, webp(img.resize((TILE, TILE), Image.BOX), _work["quality"])


LAYER_VALUE = {"tree": "grown", "bg": "on"}


_parsed = {}


def _parse_once(self, assetsfile=None):
    reader = self.deref(assetsfile)
    k = (id(reader.assets_file), reader.path_id)
    if k not in _parsed:
        _parsed[k] = reader.parse_as_object()
    return _parsed[k]


PPtr.deref_parse_as_object = _parse_once

_atlases = {}
_image_from_sprite = SpriteHelper.get_image_from_sprite


class _AtlasRef:
    def __init__(self, atlas):
        self.atlas = atlas

    def __bool__(self):
        return True

    def deref_parse_as_object(self, assetsfile=None):
        return self.atlas


def _image_with_atlas(sprite):
    if not sprite.m_SpriteAtlas and sprite.m_AtlasTags and not sprite.m_RD.texture:
        env = sprite.assets_file.environment
        if id(env) not in _atlases:
            _atlases[id(env)] = {
                o.peek_name(): o
                for f in env.files.values()
                if hasattr(f, "objects")
                for o in f.objects.values()
                if o.type.name == "SpriteAtlas"
            }
        reader = _atlases[id(env)].get(sprite.m_AtlasTags[0])
        if reader is not None:
            sprite.m_SpriteAtlas = _AtlasRef(_parse_once_reader(reader))
    return _image_from_sprite(sprite)


def _parse_once_reader(reader):
    k = (id(reader.assets_file), reader.path_id)
    if k not in _parsed:
        _parsed[k] = reader.parse_as_object()
    return _parsed[k]


SpriteHelper.get_image_from_sprite = _image_with_atlas


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--game", required=True, help="the game's IGTAPfullGame_Data folder")
    ap.add_argument("--world", required=True, help="a world export (<trace>.world.json) for which objects are on")
    ap.add_argument("--out", required=True)
    ap.add_argument("--scale", type=float, default=0.5, help="pixels per world unit at the closest zoom")
    ap.add_argument("--quality", type=int, default=80, help="WebP quality")
    ap.add_argument("--workers", type=int, default=max(4, min(16, (os.cpu_count() or 8) // 2)))
    args = ap.parse_args()
    t0 = time.time()

    env = UnityPy.load(args.game)
    files = {os.path.basename(k).lower(): f for k, f in env.files.items()}
    scene = max(
        (f for k, f in env.files.items() if os.path.basename(k).startswith("level")),
        key=lambda f: sum(o.type.name == "Tilemap" for o in f.objects.values()),
    )
    objs = scene.objects

    go, tr = {}, {}
    for pid, o in objs.items():
        n = o.type.name
        if n == "GameObject":
            t = o.read_typetree()
            go[pid] = {"name": t["m_Name"], "comps": [c["component"]["m_PathID"] for c in t["m_Component"]]}
        elif n in ("Transform", "RectTransform"):
            t = o.read_typetree()
            tr[pid] = {
                "go": t["m_GameObject"]["m_PathID"],
                "parent": t["m_Father"]["m_PathID"],
                "p": t["m_LocalPosition"],
                "r": t["m_LocalRotation"],
                "s": t["m_LocalScale"],
                "children": [c["m_PathID"] for c in t["m_Children"]],
                "rect": n == "RectTransform",
            }
    tr_of = {t["go"]: k for k, t in tr.items()}

    def comp_type(cid):
        o = objs.get(cid)
        if o is None:
            return "?"
        if o.type.name == "MonoBehaviour":
            try:
                return o.read(check_read=False).m_Script.read().m_ClassName
            except Exception:
                return "MonoBehaviour"
        return o.type.name

    types = {g: [comp_type(c) for c in v["comps"]] for g, v in go.items()}

    def key_of(tid):
        parts = []
        while tid in tr:
            t = tr[tid]
            parent = t["parent"]
            sib = tr[parent]["children"].index(tid) if parent in tr else 0
            parts.append(f"{go[t['go']]['name']}#{sib if parent in tr else ''}")
            tid = parent
        return "/".join(reversed(parts))

    scene_key = {g: key_of(t) for g, t in tr_of.items()}

    world = json.load(open(args.world, encoding="utf-8"))
    states, when_of, on = world_states(world)
    wobj = {o["id"]: o for o in world["objects"]}

    def wkey(o):
        parts = []
        while o is not None:
            parent = wobj.get(o["parent"])
            parts.append(f"{o['name']}#{o['siblingIndex'] if parent is not None else ''}")
            o = parent
        return "/".join(reversed(parts))

    export_by_key = {}
    for o in world["objects"]:
        export_by_key.setdefault(wkey(o), o["id"])
    origin = floating_origin(world)

    tree_root = {
        g
        for g, ty in types.items()
        if "TreeController" in ty or go[g]["name"] == "FirstTree" or go[g]["name"].startswith("Tree (")
    }
    tree_only = set()
    for g in go:
        tid = tr_of.get(g)
        while tid in tr:
            if tr[tid]["go"] in tree_root:
                tree_only.add(g)
                break
            tid = tr[tid]["parent"]

    renderers = {}
    for pid, o in objs.items():
        if o.type.name in ("TilemapRenderer", "SpriteRenderer"):
            t = o.read_typetree()
            renderers.setdefault(t["m_GameObject"]["m_PathID"], []).append((o, t))
    drawn, state, layer_of = {}, {}, {}
    missing = 0
    for g, k in scene_key.items():
        wid = export_by_key.get(k)
        on_now = wid is not None and wid in on
        if not on_now and g not in tree_only:
            missing += wid is None
            continue
        drawn[g] = True
        state[g] = when_of.get(wid)
        layer_of[g] = "" if on_now else "tree"
    orb_icons, orb_of = {}, {}
    for g in list(drawn):
        if "JiggleDropScript" not in types[g]:
            continue
        kind = "fullRefill" if "Full" in go[g]["name"] else "jumpRefill" if "Jump" in go[g]["name"] else "dashRefill"
        todo = list(tr[tr_of[g]]["children"])
        while todo:
            c = todo.pop(0)
            cg = tr[c]["go"]
            drawn.pop(cg, None)
            if go[cg]["name"] == "InactiveSprite":
                continue
            todo += tr[c]["children"]
            for ro, r in renderers.get(cg, []):
                if (
                    ro.type.name == "SpriteRenderer"
                    and r["m_Sprite"]["m_PathID"] != 0
                    and orb_of.setdefault(kind, g) == g
                ):
                    orb_icons.setdefault(kind, []).append(
                        (r["m_Sprite"], ro.assets_file, cg, tuple(r["m_Color"][c] for c in "rgba"), r["m_SortingOrder"])
                    )
        drawn.pop(g, None)

    def gameplay(g):
        if any("Collider2D" in t for t in types[g]) or GAMEPLAY & set(types[g]):
            return True
        name = go[g]["name"].lower()
        if "Light2D" in types[g] or any(w in name for w in DECOR_NAMES):
            return False
        tid = tr[tr_of[g]]["parent"] if g in tr_of else 0
        for _ in range(3):
            if tid not in tr:
                return False
            if GAMEPLAY & set(types[tr[tid]["go"]]):
                return True
            tid = tr[tid]["parent"]
        return False

    for g in drawn:
        if layer_of[g] == "" and not gameplay(g):
            layer_of[g] = "bg"
    print(
        f"scene: {len(go)} objects, {len(drawn)} drawn ({sum(1 for v in layer_of.values() if v)} of them trees), "
        f"{missing} not matched",
        file=sys.stderr,
    )

    rigs = {
        tr[tr[tr_of[g]]["parent"]]["go"] for g, ty in types.items() if "Movement" in ty and tr[tr_of[g]]["parent"] in tr
    }
    skip = set()
    for g in go:
        tid = tr_of.get(g)
        while tid in tr:
            gg = tr[tid]["go"]
            if (
                tr[tid]["rect"]
                or gg in rigs
                or SKIP_SCRIPTS & set(types[gg])
                or SKIP_COMPONENTS & set(types[gg]) - {"RectTransform"}
            ):
                skip.add(g)
                break
            tid = tr[tid]["parent"]

    cache = {}

    def world_tf(tid):
        if tid in cache:
            return cache[tid]
        t = tr[tid]
        q = t["r"]
        ang = 2 * math.atan2(q["z"], q["w"])
        c, s = math.cos(ang), math.sin(ang)
        m = np.array(
            [
                [c * t["s"]["x"], -s * t["s"]["y"], t["p"]["x"]],
                [s * t["s"]["x"], c * t["s"]["y"], t["p"]["y"]],
                [0, 0, 1.0],
            ]
        )
        z = t["p"]["z"]
        if t["parent"] in tr:
            pm, pz = world_tf(t["parent"])
            m, z = pm @ m, z + pz
        cache[tid] = (m, z)
        return cache[tid]

    sprites = {}

    def sprite(ptr, file):
        k = (file.name, ptr["m_FileID"], ptr["m_PathID"])
        if k not in sprites:
            ob = deref(ptr, file, files)
            sprites[k] = None
            if ob is not None:
                try:
                    sp = ob.read()
                    b = ob.read_typetree().get("m_Border") or {}
                    im = sp.image.convert("RGBA")
                    rw, rh = round(sp.m_Rect.width), round(sp.m_Rect.height)
                    if im.size != (rw, rh):
                        off = sp.m_RD.textureRectOffset
                        full = Image.new("RGBA", (rw, rh))
                        full.paste(im, (round(off.x), round(rh - off.y - im.height)))
                        im = full
                    sprites[k] = (
                        im,
                        sp.m_PixelsToUnits,
                        sp.m_Rect.width,
                        sp.m_Rect.height,
                        (sp.m_Pivot.x, sp.m_Pivot.y),
                        (b.get("x", 0), b.get("y", 0), b.get("z", 0), b.get("w", 0)),
                    )
                except Exception:
                    pass
        return k if sprites[k] else None

    pieces = {"": [], "tree": [], "bg": []}
    grids = {}
    for pid, o in objs.items():
        if o.type.name == "Grid":
            t = o.read_typetree()
            grids[t["m_GameObject"]["m_PathID"]] = t["m_CellSize"]
    for pid, o in objs.items():
        if o.type.name != "Tilemap":
            continue
        t = o.read_typetree()
        g = t["m_GameObject"]["m_PathID"]
        rs = [r for ro, r in renderers.get(g, []) if ro.type.name == "TilemapRenderer"]
        if g not in drawn or g in skip or not rs or not rs[0]["m_Enabled"] or t["m_Color"]["a"] <= 0:
            continue
        m, z = world_tf(tr_of[g])
        cell, tid = {"x": 32.0, "y": 32.0}, tr[tr_of[g]]["parent"]
        while tid in tr:
            if tr[tid]["go"] in grids:
                cell = grids[tr[tid]["go"]]
                break
            tid = tr[tid]["parent"]
        anchor = t["m_TileAnchor"]
        sx, sy = math.hypot(m[0][0], m[1][0]), math.hypot(m[0][1], m[1][1])
        base = t["m_Color"]
        for pos, td in t["m_Tiles"]:
            si = td["m_TileSpriteIndex"]
            if si >= len(t["m_TileSpriteArray"]):
                continue
            k = sprite(t["m_TileSpriteArray"][si]["m_Data"], o.assets_file)
            if k is None:
                continue
            _, ppu, rw, rh, _, _ = sprites[k]
            wx, wy, _ = m @ np.array([(pos["x"] + anchor["x"]) * cell["x"], (pos["y"] + anchor["y"]) * cell["y"], 1.0])
            mat = t["m_TileMatrixArray"][td["m_TileMatrixIndex"]]["m_Data"]
            tm = np.array([[mat["e00"], mat["e01"]], [mat["e10"], mat["e11"]]])
            tsx, tsy, tfx, rot = decompose(m[:2, :2] @ tm)
            wx, wy = (
                wx + (m[:2, :2] @ np.array([mat["e03"], mat["e13"]]))[0],
                wy + (m[:2, :2] @ np.array([mat["e03"], mat["e13"]]))[1],
            )
            col = (
                t["m_TileColorArray"][td["m_TileColorIndex"]]["m_Data"]
                if t["m_TileColorArray"]
                else {"r": 1, "g": 1, "b": 1, "a": 1}
            )
            tint = tuple(round(base[c] * col[c], 3) for c in "rgba")
            pieces[layer_of[g]].append(
                Piece(
                    rs[0]["m_SortingOrder"],
                    -z,
                    state[g],
                    layer_of[g],
                    k,
                    tint,
                    wx - origin[0],
                    wy - origin[1],
                    rw / ppu * tsx,
                    rh / ppu * tsy,
                    tfx,
                    False,
                    rot,
                    "",
                )
            )
    for g, rl in renderers.items():
        for ro, r in rl:
            if (
                ro.type.name != "SpriteRenderer"
                or g not in drawn
                or g in skip
                or not r["m_Enabled"]
                or r["m_Sprite"]["m_PathID"] == 0
            ):
                continue
            k = sprite(r["m_Sprite"], ro.assets_file)
            if k is None or r["m_Color"]["a"] <= 0:
                continue
            _, ppu, rw, rh, _, _ = sprites[k]
            m, z = world_tf(tr_of[g])
            sx, sy = math.hypot(m[0][0], m[1][0]), math.hypot(m[0][1], m[1][1])
            sx, sy, mirrored, ang = decompose(
                m[:2, :2] @ np.diag([-1.0 if r["m_FlipX"] else 1.0, -1.0 if r["m_FlipY"] else 1.0])
            )
            w, h = (r["m_Size"]["x"], r["m_Size"]["y"]) if r["m_DrawMode"] != 0 else (rw / ppu, rh / ppu)
            pieces[layer_of[g]].append(
                Piece(
                    r["m_SortingOrder"],
                    -z,
                    state[g],
                    layer_of[g],
                    k,
                    tuple(round(r["m_Color"][c], 3) for c in "rgba"),
                    m[0][2] - origin[0],
                    m[1][2] - origin[1],
                    w * sx,
                    h * sy,
                    mirrored,
                    False,
                    ang,
                    {1: "slice", 2: "tile"}.get(r["m_DrawMode"], ""),
                )
            )
    for layer in pieces:
        pieces[layer].sort(key=lambda p: (p.order, -p.z))
    icons = {}
    for kind, parts in orb_icons.items():
        drawn_parts = []
        for ptr, file, cg, tint, order in sorted(parts, key=lambda q: q[4]):
            k = sprite(ptr, file)
            if k is None:
                continue
            im, ppu, rw, rh, pv, _ = sprites[k]
            m, _ = world_tf(tr_of[cg])
            sx, sy, _, _ = decompose(m[:2, :2])
            w, h = rw / ppu * sx, rh / ppu * sy
            if tint != (1, 1, 1, 1):
                im = Image.fromarray(
                    (np.asarray(im).astype(np.float32) * np.array(tint, dtype=np.float32)).clip(0, 255).astype(np.uint8)
                )
            drawn_parts.append((im, m[0][2] - pv[0] * w, m[1][2] - pv[1] * h, w, h))
        if not drawn_parts:
            continue
        x0 = min(q[1] for q in drawn_parts)
        y0 = min(q[2] for q in drawn_parts)
        x1 = max(q[1] + q[3] for q in drawn_parts)
        y1 = max(q[2] + q[4] for q in drawn_parts)
        canvas = Image.new("RGBA", (max(1, round((x1 - x0) * 2)), max(1, round((y1 - y0) * 2))))
        for im, x, y, w, h in drawn_parts:
            part = im.resize((max(1, round(w * 2)), max(1, round(h * 2))), Image.LANCZOS)
            paste(canvas, part, round((x - x0) * 2), round((y1 - y - h) * 2))
        om, _ = world_tf(tr_of[orb_of[kind]])
        icons[kind] = (canvas, x1 - x0, y1 - y0, (x0 + x1) / 2 - om[0][2], (y0 + y1) / 2 - om[1][2])
    print(
        f"{sum(len(v) for v in pieces.values())} pieces ({len(sprites)} sprites) in {time.time() - t0:.1f}s",
        file=sys.stderr,
    )

    render(pieces, sprites, states, args.scale, args.quality, args.workers, args.out, icons)


def decompose(m):
    sx, sy = math.hypot(m[0][0], m[1][0]), math.hypot(m[0][1], m[1][1])
    flip = m[0][0] * m[1][1] - m[0][1] * m[1][0] < 0
    ang = math.degrees(math.atan2(-m[1][0], -m[0][0]) if flip else math.atan2(m[1][0], m[0][0]))
    ang = round(ang, 1) % 360
    return sx, sy, flip, 0 if min(ang, 360 - ang) < 0.5 else (ang if ang <= 180 else ang - 360)


def deref(ptr, file, files):
    fid, path = ptr["m_FileID"], ptr["m_PathID"]
    if path == 0:
        return None
    if fid != 0:
        ext = file.externals[fid - 1]
        file = files.get(os.path.basename(ext.path).lower())
        if file is None:
            return None
    return file.objects.get(path)


def floating_origin(world):
    for o in world["objects"]:
        for c in o["components"]:
            if c["type"] == "FloatingOrigin":
                for f in c.get("fields", []):
                    if f[1] == "currentOrigin" and isinstance(f[2], dict):
                        return f[2]["x"], f[2]["y"]
    return 0.0, 0.0


def world_states(world):
    objects = world["objects"]
    active = {o["id"]: o["activeSelf"] for o in objects}
    owner, comp, kids = {}, {}, collections.defaultdict(list)
    for o in objects:
        kids[o["parent"]].append(o["id"])
        for c in o["components"]:
            owner[c["id"]], comp[c["id"]] = o["id"], c

    def boxes_under(i):
        out = [cid for cid, c in comp.items() if owner[cid] == i and c["type"] == "upgradeBox"]
        for k in kids[i]:
            out += boxes_under(k)
        return out

    roots, states = {}, []
    zoned = set()
    for o in objects:
        for c in o["components"]:
            fields = {f[1]: f[2] for f in c.get("fields", [])}
            refs = lambda name: [
                r["$ref"]
                for r in (lambda v: v if isinstance(v, list) else [v])(fields.get(name) or [])
                if isinstance(r, dict) and "$ref" in r and (r["$ref"] in active or r["$ref"] in owner)
            ]
            if c["type"] == "Zone2to1TransitionController":
                roots.update({i: "area1:normal|vman" for i in refs("closedObject")})
                roots.update({i: "area1:overgrown" for i in refs("openObject") + refs("trunks")[:1]})
                keep = set(refs("closedObject") + refs("trunks"))
                roots.update({k: "area1:overgrown" for k in kids[o["id"]] if k not in keep and k not in roots})
            if c["type"] == "OvergrowthLoadScript":
                over, normal = refs("overgrowthEnable"), refs("overgrowthDisable")
                roots.update({i: "area1:overgrown" for i in over})
                roots.update({i: "area1:normal|vman" for i in normal})
                for course in refs("courses"):
                    for box in boxes_under(owner.get(course, course)):
                        f = {x[1]: x[2] for x in comp[box].get("fields", [])}
                        if f.get("upgrade") in (1, 6, 10) or f.get("upgrade") == 0 and f.get("globalUpgrade") == 14:
                            roots[owner[box]] = "area1:normal|vman"
                for box in refs("boxesToEnable") + refs("rpAtomBox"):
                    if box in owner:
                        roots[owner[box]] = "area1:overgrown"
                vman = [
                    owner[cid]
                    for cid, cc in comp.items()
                    if cc["type"] == "upgradeBox"
                    and {x[1]: x[2] for x in cc.get("fields", [])}.get("upgrade") == 1
                    and {x[1]: x[2] for x in cc.get("fields", [])}.get("movementUpgrade") == 7
                ]
                roots.update({i: "area1:vman" for i in vman})
                states.append(
                    {
                        "id": "area1",
                        "options": ["normal", "overgrown", "vman"],
                        "current": (
                            "overgrown"
                            if any(active[i] for i in over)
                            else "vman" if any(active.get(i) for i in vman) else "normal"
                        ),
                    }
                )
            if c["type"] in ("ZoneLoader", "ZoneDisableLoadList"):
                zoned.update(refs("allZones") + refs("toLoad"))
    zoned |= set(roots)
    parent = {o["id"]: o["parent"] for o in objects}
    known = {}

    def is_on(i):
        if i == 0 or i not in parent:
            return True
        if i not in known:
            known[i] = (active[i] or i in zoned) and is_on(parent[i])
        return known[i]

    on = {i for i in parent if is_on(i)}
    when = {}
    for start in parent:
        i = start
        while i and i in parent:
            if i in roots:
                when[start] = roots[i]
                break
            i = parent[i]
    return states, when, on


def combos(states):
    out = [{}]
    for s in states:
        out = [dict(p, **{s["id"]: o}) for p in out for o in s["options"]]
    return out


def art_key(pick):
    return ",".join(f"{k}:{pick[k]}" for k in sorted(pick))


def render(pieces, sprites, states, scale, quality, workers, out, icons):
    os.makedirs(out, exist_ok=True)
    span = TILE / scale
    margin = 2048
    xs = [p.x for pl in pieces.values() for p in pl]
    ys = [p.y for pl in pieces.values() for p in pl]
    bi0, bi1 = math.floor((min(xs) - margin) / span), math.floor((max(xs) + margin) / span)
    bj0, bj1 = math.floor((min(ys) - margin) / span), math.floor((max(ys) + margin) / span)
    index = {"tile": TILE, "scale": scale, "states": {}, "icons": {}}
    written = {}

    def save(data):
        h = hashlib.sha1(data).hexdigest()[:16]
        if h not in written:
            written[h] = len(data)
            with open(os.path.join(out, h + ".webp"), "wb") as f:
                f.write(data)
        return h

    payload = os.path.join(out, ".sprites.pkl")
    with open(payload, "wb") as f:
        pickle.dump({k: (s[0].tobytes(), s[0].size) + tuple(s[1:]) for k, s in sprites.items() if s}, f)
    t = time.time()
    with multiprocessing.Pool(workers, _worker_init, (payload, scale, quality, TILE)) as pool:
        for pick in combos(states):
            for layer, plist in pieces.items():
                if not plist:
                    continue
                shown = lambda p: p.state is None or pick.get(p.state.split(":")[0]) in p.state.split(":")[1].split("|")
                key = art_key(pick if not layer else dict(pick, **{layer: LAYER_VALUE[layer]}))
                buckets = collections.defaultdict(list)
                for n, p in enumerate(plist):
                    r = max(p.w, p.h)
                    for i in range(max(math.floor((p.x - r) / span), bi0), min(math.floor((p.x + r) / span), bi1) + 1):
                        for j in range(
                            max(math.floor((p.y - r) / span), bj0), min(math.floor((p.y + r) / span), bj1) + 1
                        ):
                            buckets[(i, j)].append(n)
                tasks = [
                    (i, j, span, [tuple(plist[n]) for n in sorted(bucket) if shown(plist[n])])
                    for (i, j), bucket in sorted(buckets.items())
                ]
                levels, cur = [], {}
                for i, j, data in pool.imap_unordered(_tile_task, tasks, chunksize=2):
                    if data:
                        cur[(i, j)] = data
                    if len(cur) % 200 == 0:
                        print(f"{key}: {len(cur)}/{len(tasks)} tiles", file=sys.stderr, flush=True)
                while True:
                    levels.append({f"{i},{j}": h for (i, j), d in cur.items() if (h := save(d))})
                    print(
                        f"{key}: level {len(levels) - 1} of {len(cur)} tiles in {time.time() - t:.1f}s, "
                        f"{len(written)} files, {sum(written.values()) / 1e6:.1f} MB",
                        file=sys.stderr,
                        flush=True,
                    )
                    t = time.time()
                    if len(cur) <= 1 or len(levels) >= LEVELS:
                        break
                    groups = collections.defaultdict(list)
                    for ij in cur:
                        groups[(ij[0] // 2, ij[1] // 2)].append(ij)
                    tasks = [
                        (i, j, [(c[0] - 2 * i, c[1] - 2 * j, cur[c]) for c in cs])
                        for (i, j), cs in sorted(groups.items())
                    ]
                    cur = {
                        (i, j): data for i, j, data in pool.imap_unordered(_pyramid_task, tasks, chunksize=2) if data
                    }
                index["states"][key] = levels
    os.remove(payload)
    for kind, (im, w, h, dx, dy) in icons.items():
        buf = io.BytesIO()
        im.save(buf, "WEBP", quality=90)
        index["icons"][kind] = {
            "file": save(buf.getvalue()),
            "w": round(w, 1),
            "h": round(h, 1),
            "dx": round(dx, 1),
            "dy": round(dy, 1),
        }
    with open(os.path.join(out, "index.json"), "w") as f:
        json.dump(index, f, separators=(",", ":"))
    print(f"done: {len(written)} files, {sum(written.values()) / 1e6:.1f} MB", file=sys.stderr)


def resize(im, size):
    return im.resize(size, Image.BOX if size[0] < im.width or size[1] < im.height else Image.BILINEAR)


def slice9(im, border, out, W, H):
    l, b, r, t = border
    ol, ob, orr, ot = out
    iw, ih = im.size
    if not any(border) or l + r >= min(W, iw) or t + b >= min(H, ih):
        return resize(im, (W, H))
    dst = Image.new("RGBA", (W, H))
    xs, ys = (0, l, iw - r), (0, t, ih - b)
    sw, sh = (l, iw - l - r, r), (t, ih - t - b, b)
    xw, yh = (ol, W - ol - orr, orr), (ot, H - ot - ob, ob)
    xo, yo = (0, ol, W - orr), (0, ot, H - ob)
    for yi in range(3):
        for xi in range(3):
            if xw[xi] <= 0 or yh[yi] <= 0:
                continue
            part = resize(im.crop((xs[xi], ys[yi], xs[xi] + sw[xi], ys[yi] + sh[yi])), (xw[xi], yh[yi]))
            dst.paste(part, (xo[xi], yo[yi]))
    return dst


def paste(dst, src, x, y):
    sx0, sy0 = max(0, -x), max(0, -y)
    sx1, sy1 = min(src.width, dst.width - x), min(src.height, dst.height - y)
    if sx1 <= sx0 or sy1 <= sy0:
        return
    dst.alpha_composite(src.crop((sx0, sy0, sx1, sy1)), (x + sx0, y + sy0))


if __name__ == "__main__":
    main()
