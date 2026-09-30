using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace IGTAP.EngineSim.Ultras
{
    static class Level
    {
        internal sealed record Marker(string kind, string name, float x, float y,
            [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] float[][] area = null,
            [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string when = null,
            [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] float[] spawn = null,
            [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BoxInfo box = null);

        static float Round(float v) { return (float)Math.Round(v, 1); }

        internal sealed record WorldShape(string Type, float[] V, float R, int Layer, string Tag, bool Trigger, bool Spike, bool Moving, bool Player, string Tone = null, float[] Up = null, string When = null, int[] Rings = null);

        internal sealed record WorldState(string id, string label, string[] options, string current);

        static readonly Dictionary<(JsonElement, string), object> Memo = new Dictionary<(JsonElement, string), object>();
        static T Remember<T>(JsonElement world, string what, Func<T> make)
        {
            lock (Memo)
                if (Memo.TryGetValue((world, what), out object v)) return (T)v;
            T made = make();
            lock (Memo) Memo[(world, what)] = made;
            return made;
        }

        internal static (List<WorldState> states, Dictionary<int, string> roots) WorldStates(JsonElement world) =>
            Remember(world, "states", () => MakeWorldStates(world));

        static (List<WorldState> states, Dictionary<int, string> roots) MakeWorldStates(JsonElement world)
        {
            var states = new List<WorldState>();
            var roots = new Dictionary<int, string>();
            var active = new Dictionary<int, bool>();
            foreach (JsonElement o in world.GetProperty("objects").EnumerateArray()) active[o.GetProperty("id").GetInt32()] = o.GetProperty("activeSelf").GetBoolean();
            var owner = new Dictionary<int, int>();
            var components = new Dictionary<int, JsonElement>();
            var children = new Dictionary<int, List<int>>();
            foreach (JsonElement o in world.GetProperty("objects").EnumerateArray())
            {
                int id = o.GetProperty("id").GetInt32();
                int parent = o.GetProperty("parent").GetInt32();
                if (!children.TryGetValue(parent, out var list)) children[parent] = list = new List<int>();
                list.Add(id);
                foreach (JsonElement c in o.GetProperty("components").EnumerateArray())
                {
                    owner[c.GetProperty("id").GetInt32()] = id;
                    components[c.GetProperty("id").GetInt32()] = c;
                }
            }
            var boxesOf = components.Where(kv => kv.Value.GetProperty("type").GetString() == "upgradeBox").ToLookup(kv => owner[kv.Key], kv => kv.Key);
            IEnumerable<int> BoxesUnder(int id)
            {
                foreach (int cid in boxesOf[id]) yield return cid;
                if (children.TryGetValue(id, out var kids))
                    foreach (int k in kids)
                        foreach (int b in BoxesUnder(k)) yield return b;
            }
            foreach (JsonElement o in world.GetProperty("objects").EnumerateArray())
                foreach (JsonElement c in o.GetProperty("components").EnumerateArray())
                {
                    string type = c.GetProperty("type").GetString();
                    if (type != "OvergrowthLoadScript" && type != "Zone2to1TransitionController") continue;
                    var f = Fields(c);
                    List<int> Refs(string name) => !f.TryGetValue(name, out JsonElement v) ? new List<int>()
                        : (v.ValueKind == JsonValueKind.Array ? v.EnumerateArray() : new[] { v }.AsEnumerable())
                            .Where(r => r.ValueKind == JsonValueKind.Object && r.TryGetProperty("$ref", out _)).Select(r => r.GetProperty("$ref").GetInt32()).Where(id => active.ContainsKey(id) || owner.ContainsKey(id)).ToList();
                    if (type == "Zone2to1TransitionController")
                    {
                        foreach (int id in Refs("closedObject")) roots[id] = "area1:normal|vman";
                        foreach (int id in Refs("openObject").Concat(Refs("trunks").Take(1))) roots[id] = "area1:overgrown";
                        var keep = new HashSet<int>(Refs("closedObject").Concat(Refs("trunks")));
                        int self = owner.TryGetValue(c.GetProperty("id").GetInt32(), out int so) ? so : o.GetProperty("id").GetInt32();
                        if (children.TryGetValue(self, out var under))
                            foreach (int k in under) if (!keep.Contains(k) && !roots.ContainsKey(k)) roots[k] = "area1:overgrown";
                        continue;
                    }
                    List<int> overgrown = Refs("overgrowthEnable"), normal = Refs("overgrowthDisable");
                    foreach (int id in overgrown) roots[id] = "area1:overgrown";
                    foreach (int id in normal) roots[id] = "area1:normal|vman";
                    foreach (int course in Refs("courses"))
                        foreach (int box in BoxesUnder(owner.TryGetValue(course, out int co) ? co : course))
                            if (Fields(components[box]) is var bf && bf.TryGetValue("upgrade", out JsonElement up) && up.ValueKind == JsonValueKind.Number
                                && (up.GetInt32() is 1 or 6 or 10 || up.GetInt32() == 0 && bf.TryGetValue("globalUpgrade", out JsonElement gu) && gu.ValueKind == JsonValueKind.Number && gu.GetInt32() == 14))
                                roots[owner[box]] = "area1:normal|vman";
                    foreach (int box in Refs("boxesToEnable").Append(Refs("rpAtomBox").FirstOrDefault()).Where(b => b != 0))
                        if (owner.TryGetValue(box, out int bo)) roots[bo] = "area1:overgrown";
                    var vmanBoxes = components.Where(kv => kv.Value.GetProperty("type").GetString() == "upgradeBox" && Fields(kv.Value) is var vf
                        && vf.TryGetValue("upgrade", out JsonElement vu) && vu.ValueKind == JsonValueKind.Number && vu.GetInt32() == 1
                        && vf.TryGetValue("movementUpgrade", out JsonElement vm) && vm.ValueKind == JsonValueKind.Number && vm.GetInt32() == 7).Select(kv => owner[kv.Key]).ToList();
                    foreach (int id in vmanBoxes) roots[id] = "area1:vman";
                    states.Add(new WorldState("area1", "Area 1", new[] { "normal", "overgrown", "vman" },
                        overgrown.Any(id => active[id]) ? "overgrown" : vmanBoxes.Any(id => active.TryGetValue(id, out bool on) && on) ? "vman" : "normal"));
                }
            return (states, roots);
        }

        internal static HashSet<int> OnObjects(JsonElement world, bool allStates) =>
            Remember(world, allStates ? "on:all" : "on", () => MakeOnObjects(world, allStates));

        static HashSet<int> MakeOnObjects(JsonElement world, bool allStates)
        {
            var on = new HashSet<int>();
            var objects = world.GetProperty("objects").EnumerateArray().ToList();
            if (!allStates)
            {
                foreach (JsonElement o in objects) if (o.GetProperty("activeInHierarchy").GetBoolean()) on.Add(o.GetProperty("id").GetInt32());
                return on;
            }
            var zoned = new HashSet<int>(WorldStates(world).roots.Keys);
            foreach (JsonElement o in objects)
                foreach (JsonElement c in o.GetProperty("components").EnumerateArray())
                {
                    string type = c.GetProperty("type").GetString();
                    if ((type != "ZoneLoader" && type != "ZoneDisableLoadList") || !c.TryGetProperty("fields", out JsonElement fields)) continue;
                    foreach (JsonElement f in fields.EnumerateArray())
                        if ((f[1].GetString() == "allZones" || f[1].GetString() == "toLoad") && f[2].ValueKind == JsonValueKind.Array)
                            foreach (JsonElement r in f[2].EnumerateArray())
                                if (r.ValueKind == JsonValueKind.Object && r.TryGetProperty("$ref", out JsonElement id)) zoned.Add(id.GetInt32());
                }
            var nodes = objects.ToDictionary(o => o.GetProperty("id").GetInt32(),
                o => (parent: o.GetProperty("parent").GetInt32(), self: o.GetProperty("activeSelf").GetBoolean()));
            var known = new Dictionary<int, bool>();
            bool On(int id)
            {
                if (id == 0 || !nodes.TryGetValue(id, out var n)) return true;
                if (known.TryGetValue(id, out bool v)) return v;
                return known[id] = (n.self || zoned.Contains(id)) && On(n.parent);
            }
            foreach (int id in nodes.Keys) if (On(id)) on.Add(id);
            return on;
        }

        static Dictionary<int, string> StateOf(JsonElement world) => Remember(world, "when", () => MakeStateOf(world));

        static Dictionary<int, string> MakeStateOf(JsonElement world)
        {
            Dictionary<int, string> roots = WorldStates(world).roots;
            var parent = new Dictionary<int, int>();
            foreach (JsonElement o in world.GetProperty("objects").EnumerateArray()) parent[o.GetProperty("id").GetInt32()] = o.GetProperty("parent").GetInt32();
            var when = new Dictionary<int, string>();
            foreach (int start in parent.Keys)
                for (int id = start; id != 0 && parent.ContainsKey(id); id = parent[id])
                    if (roots.TryGetValue(id, out string state)) { when[start] = state; break; }
            return when;
        }

        static Dictionary<int, (float x, float y, float cos, float sin, bool moving)> Bodies(JsonElement world, Vector3 origin)
        {
            var bodies = new Dictionary<int, (float x, float y, float cos, float sin, bool moving)>();
            foreach (JsonElement o in world.GetProperty("objects").EnumerateArray())
                foreach (JsonElement c in o.GetProperty("components").EnumerateArray())
                    if (c.GetProperty("type").GetString() == "UnityEngine.Rigidbody2D" && c.TryGetProperty("props", out JsonElement bp))
                    {
                        JsonElement pos = bp.GetProperty("position");
                        double angle = bp.GetProperty("rotation").GetDouble() * Math.PI / 180.0;
                        bool moving = bp.TryGetProperty("bodyType", out JsonElement bt) && bt.GetInt32() != (int)RigidbodyType2D.Static;
                        bodies[c.GetProperty("id").GetInt32()] = (pos.GetProperty("x").GetSingle() - origin.x, pos.GetProperty("y").GetSingle() - origin.y, (float)Math.Cos(angle), (float)Math.Sin(angle), moving);
                    }
            return bodies;
        }

        static void ToWorld(float[] v, (float x, float y, float cos, float sin, bool moving)? body, Vector3 origin)
        {
            if (body is { } b)
                for (int i = 0; i + 1 < v.Length; i += 2)
                {
                    float lx = v[i], ly = v[i + 1];
                    v[i] = b.x + b.cos * lx - b.sin * ly;
                    v[i + 1] = b.y + b.sin * lx + b.cos * ly;
                }
            else
                for (int i = 0; i + 1 < v.Length; i += 2) { v[i] -= origin.x; v[i + 1] -= origin.y; }
        }

        static (float x, float y, float cos, float sin, bool moving)? BodyOf(JsonElement props, Dictionary<int, (float x, float y, float cos, float sin, bool moving)> bodies)
        {
            return props.TryGetProperty("attachedRigidbody", out JsonElement rb) && rb.ValueKind == JsonValueKind.Object
                && rb.TryGetProperty("$ref", out JsonElement id) && bodies.TryGetValue(id.GetInt32(), out var found) ? found : null;
        }

        internal static List<WorldShape> Shapes(JsonElement world, Vector3 origin, bool allStates)
        {
            var bodies = Bodies(world, origin);
            HashSet<int> on = OnObjects(world, allStates);
            Dictionary<int, string> when = StateOf(world);
            var tones = new Dictionary<int, string>();
            foreach (JsonElement o in world.GetProperty("objects").EnumerateArray())
                foreach (JsonElement c in o.GetProperty("components").EnumerateArray())
                    if (c.GetProperty("type").GetString() == "colouredBlockSwapper" && c.TryGetProperty("fields", out JsonElement fields))
                        foreach (JsonElement f in fields.EnumerateArray())
                            if ((f[1].GetString() == "blue" || f[1].GetString() == "orange") && f[2].ValueKind == JsonValueKind.Array)
                                foreach (JsonElement r in f[2].EnumerateArray())
                                    if (r.ValueKind == JsonValueKind.Object && r.TryGetProperty("$ref", out JsonElement id)) tones[id.GetInt32()] = f[1].GetString();
            var shapesOut = new List<WorldShape>();
            foreach (JsonElement o in world.GetProperty("objects").EnumerateArray())
            {
                int oid = o.GetProperty("id").GetInt32();
                if (!on.Contains(oid)) continue;
                bool live = o.GetProperty("activeInHierarchy").GetBoolean();
                string state = when.TryGetValue(oid, out string w) ? w : null;
                string tone = tones.TryGetValue(oid, out string t0) ? t0 : null;
                var components = o.GetProperty("components").EnumerateArray().ToList();
                bool spike = components.Any(c => c.GetProperty("type").GetString() == "spikeScript");
                bool player = components.Any(c => c.GetProperty("type").GetString() == "Movement");
                float[] up = null;
                if (components.Any(c => c.GetProperty("type").GetString() == "SpringScript"))
                {
                    JsonElement q = o.GetProperty("rotation");
                    float qx = q.GetProperty("x").GetSingle(), qy = q.GetProperty("y").GetSingle(), qz = q.GetProperty("z").GetSingle(), qw = q.GetProperty("w").GetSingle();
                    up = new[] { 2f * (qx * qy - qw * qz), 1f - 2f * (qx * qx + qz * qz) };
                }
                int layer = o.GetProperty("layer").GetInt32();
                string tag = o.GetProperty("tag").GetString();
                foreach (JsonElement c in components)
                {
                    bool both = allStates && tone != null && c.GetProperty("type").GetString() == "UnityEngine.CompositeCollider2D";
                    if (!c.TryGetProperty(both ? "sourceShapes" : "shapes", out JsonElement shapes) || shapes.ValueKind != JsonValueKind.Object) continue;
                    if (!both && c.TryGetProperty("enabled", out JsonElement enabled) && !enabled.GetBoolean()) continue;
                    JsonElement props = c.GetProperty("props");
                    bool trigger = props.TryGetProperty("isTrigger", out JsonElement t) && t.GetBoolean();
                    (float x, float y, float cos, float sin, bool moving)? body = BodyOf(props, bodies);
                    var loops = new List<(string type, float[] v, float r, int[] rings)>();
                    foreach (JsonElement s in shapes.GetProperty("list").EnumerateArray())
                        loops.Add((s.GetProperty("type").GetString(), s.GetProperty("vertices").EnumerateArray().Select(e => e.GetSingle()).ToArray(),
                            s.TryGetProperty("radius", out JsonElement rad) ? rad.GetSingle() : 0f, null));
                    if (!live && loops.Count == 0 && c.TryGetProperty("paths", out JsonElement paths) && paths.ValueKind == JsonValueKind.Array)
                    {
                        float[][] rings = paths.EnumerateArray().Select(path => path.EnumerateArray().Select(e => e.GetSingle()).ToArray()).Where(p => p.Length >= 6).ToArray();
                        if (rings.Length > 0) loops.Add(("Area", rings.SelectMany(p => p).ToArray(), 0f, rings.Select(p => p.Length).ToArray()));
                    }
                    foreach (var (type, v, r, rings) in loops)
                    {
                        if (v.Length < 2) continue;
                        ToWorld(v, body, origin);
                        shapesOut.Add(new WorldShape(type, v, r, layer, tag, trigger, spike, body is { moving: true }, player, tone, up, state, rings));
                    }
                }
            }
            return shapesOut;
        }

        internal static (float x0, float y0, float x1, float y1) LevelBounds(JsonElement world, Vector3 origin)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (WorldShape s in Shapes(world, origin, allStates: true))
            {
                if (s.Player || s.Trigger && !s.Spike && s.Up == null) continue;
                for (int i = 0; i + 1 < s.V.Length; i += 2)
                {
                    x0 = Math.Min(x0, s.V[i] - s.R); x1 = Math.Max(x1, s.V[i] + s.R);
                    y0 = Math.Min(y0, s.V[i + 1] - s.R); y1 = Math.Max(y1, s.V[i + 1] + s.R);
                }
            }
            return (x0, y0, x1, y1);
        }

        internal static List<object> Solids(JsonElement world, Vector3 origin, float x0, float y0, float x1, float y1)
        {
            var solids = new List<object>();
            foreach (WorldShape s in Shapes(world, origin, allStates: true))
            {
                if (s.Trigger && !s.Spike && s.Up == null) continue;
                float[] v = s.V;
                float r = s.R;
                float sx0 = float.MaxValue, sy0 = float.MaxValue, sx1 = float.MinValue, sy1 = float.MinValue;
                for (int i = 0; i + 1 < v.Length; i += 2) { sx0 = Math.Min(sx0, v[i]); sx1 = Math.Max(sx1, v[i]); sy0 = Math.Min(sy0, v[i + 1]); sy1 = Math.Max(sy1, v[i + 1]); }
                if (sx1 + r < x0 || sx0 - r > x1 || sy1 + r < y0 || sy0 - r > y1) continue;
                string k = s.Type == "Polygon" ? "poly" : s.Type == "Area" ? "area" : s.Type == "Circle" ? "circle" : s.Type == "Capsule" ? "capsule" : "line";
                solids.Add(new { k, spike = s.Spike, p = v.Select(Round), r, tone = s.Tone, spring = s.Up?.Select(e => (float)Math.Round(e, 3)), when = s.When, rings = s.Rings });
            }
            return solids;
        }

        internal sealed record TreeGround(List<object> Shapes, List<object> Steps);

        internal static TreeGround Grown(JsonElement world, Vector3 origin, float x0, float y0, float x1, float y1)
        {
            var objects = world.GetProperty("objects").EnumerateArray().ToList();
            var byId = objects.ToDictionary(o => o.GetProperty("id").GetInt32());
            var owner = new Dictionary<int, int>();
            var components = new Dictionary<int, JsonElement>();
            foreach (JsonElement o in objects)
                foreach (JsonElement c in o.GetProperty("components").EnumerateArray())
                {
                    owner[c.GetProperty("id").GetInt32()] = o.GetProperty("id").GetInt32();
                    components[c.GetProperty("id").GetInt32()] = c;
                }
            JsonElement? Field(JsonElement c, string name)
            {
                if (c.TryGetProperty("fields", out JsonElement fields))
                    foreach (JsonElement f in fields.EnumerateArray()) if (f[1].GetString() == name) return f[2];
                return null;
            }
            int Ref(JsonElement? v) => v is { ValueKind: JsonValueKind.Object } r && r.TryGetProperty("$ref", out JsonElement id) ? id.GetInt32() : 0;
            string Name(int objectId) => byId.TryGetValue(objectId, out JsonElement o) ? o.GetProperty("name").GetString() : "?";

            var steps = new Dictionary<int, (int tree, int step, string via, string box)>();
            foreach (JsonElement o in objects)
                foreach (JsonElement tc in o.GetProperty("components").EnumerateArray())
                {
                    if (tc.GetProperty("type").GetString() != "TreeController" || Field(tc, "TreeSegments") is not { ValueKind: JsonValueKind.Array } list) continue;
                    int treeId = tc.GetProperty("id").GetInt32(), tree = Field(tc, "treeNumber")?.GetInt32() ?? 0;
                    var segs = list.EnumerateArray().Select(r => components.TryGetValue(Ref(r), out JsonElement s) ? s : (JsonElement?)null).ToList();
                    var triggers = new Dictionary<int, int[]>();
                    foreach (JsonElement b in components.Values)
                        if (b.GetProperty("type").GetString() == "upgradeBox" && Ref(Field(b, "upgradeTree")) == treeId && Field(b, "SegmentsToTrigger") is { ValueKind: JsonValueKind.Array } grows)
                            triggers[owner[b.GetProperty("id").GetInt32()]] = grows.EnumerateArray().Select(e => e.GetInt32()).ToArray();
                    var queue = new Queue<(int box, int step)>();
                    int plant = owner.TryGetValue(Ref(Field(tc, "plantTreeBox")), out int p) ? p : 0;
                    queue.Enqueue((plant, 1));
                    var seen = new HashSet<int>();
                    void Grow(int index, int step, int via)
                    {
                        if (index < 0 || index >= segs.Count || segs[index] is not { } s || !seen.Add(index)) return;
                        int box = Ref(Field(s, "box"));
                        steps[owner[s.GetProperty("id").GetInt32()]] = (tree, step, Name(via), box != 0 ? Name(box) : null);
                        if (box != 0) queue.Enqueue((box, step + 1));
                        if (Field(s, "autoTriggerNext") is { ValueKind: JsonValueKind.True }) Grow(Field(s, "NextSegmentToAutoTriggerIndex")?.GetInt32() ?? -1, step, via);
                    }
                    while (queue.Count > 0)
                    {
                        var (box, step) = queue.Dequeue();
                        if (triggers.TryGetValue(box, out int[] grows)) foreach (int index in grows) Grow(index, step, box);
                    }
                    for (int index = 0; index < segs.Count; index++)
                        if (segs[index] is { } s && !seen.Contains(index))
                            steps[owner[s.GetProperty("id").GetInt32()]] = (tree, 0, null, Ref(Field(s, "box")) is var b and not 0 ? Name(b) : null);
                }

            var parent = objects.ToDictionary(o => o.GetProperty("id").GetInt32(), o => o.GetProperty("parent").GetInt32());
            int SegmentOf(int id)
            {
                for (; id != 0 && parent.ContainsKey(id); id = parent[id]) if (steps.ContainsKey(id)) return id;
                return 0;
            }
            var bodies = Bodies(world, origin);
            HashSet<int> on = OnObjects(world, allStates: true);
            var shapes = new List<object>();
            var extent = new Dictionary<int, (float x0, float y0, float x1, float y1)>();
            foreach (JsonElement o in objects)
            {
                int oid = o.GetProperty("id").GetInt32(), segment = SegmentOf(oid);
                if (on.Contains(oid) || segment == 0) continue;
                foreach (JsonElement c in o.GetProperty("components").EnumerateArray())
                {
                    if (!c.TryGetProperty("props", out JsonElement props) || !props.TryGetProperty("isTrigger", out JsonElement trigger) || trigger.GetBoolean()) continue;
                    if (props.TryGetProperty("compositeOperation", out JsonElement op) && op.ValueKind == JsonValueKind.Number && op.GetInt32() != 0) continue;
                    var polygons = new List<float[]>();
                    if (c.TryGetProperty("paths", out JsonElement paths) && c.GetProperty("type").GetString() == "UnityEngine.CompositeCollider2D")
                        foreach (JsonElement path in paths.EnumerateArray()) polygons.Add(path.EnumerateArray().Select(e => e.GetSingle()).ToArray());
                    else if (c.TryGetProperty("shapes", out JsonElement list) && list.ValueKind == JsonValueKind.Object)
                        foreach (JsonElement s in list.GetProperty("list").EnumerateArray())
                            if (s.GetProperty("type").GetString() == "Polygon") polygons.Add(s.GetProperty("vertices").EnumerateArray().Select(e => e.GetSingle()).ToArray());
                    var body = BodyOf(props, bodies);
                    foreach (float[] v in polygons.Where(v => v.Length >= 6))
                    {
                        ToWorld(v, body, origin);
                        float sx0 = float.MaxValue, sy0 = float.MaxValue, sx1 = float.MinValue, sy1 = float.MinValue;
                        for (int i = 0; i + 1 < v.Length; i += 2) { sx0 = Math.Min(sx0, v[i]); sx1 = Math.Max(sx1, v[i]); sy0 = Math.Min(sy0, v[i + 1]); sy1 = Math.Max(sy1, v[i + 1]); }
                        if (sx1 < x0 || sx0 > x1 || sy1 < y0 || sy0 > y1) continue;
                        shapes.Add(new { k = "poly", spike = false, p = v.Select(Round), r = 0f });
                        extent[segment] = extent.TryGetValue(segment, out var e) ? (Math.Min(e.x0, sx0), Math.Min(e.y0, sy0), Math.Max(e.x1, sx1), Math.Max(e.y1, sy1)) : (sx0, sy0, sx1, sy1);
                    }
                }
            }
            var labels = extent.Select(kv => (object)new
            {
                tree = steps[kv.Key].tree,
                step = steps[kv.Key].step,
                via = steps[kv.Key].via,
                box = steps[kv.Key].box,
                x = Round((kv.Value.x0 + kv.Value.x1) / 2f),
                y = Round((kv.Value.y0 + kv.Value.y1) / 2f)
            }).ToList();
            return new TreeGround(shapes, labels);
        }

        static readonly string[] Currencies = { "W", "GP", "NP", "number", "CD", "RP", "BP" };

        static readonly string[] BoxLocal = { "GLOBAL", "Movement", "cloneCount", "cashPerLoop", "fastCloneChance", "bigCloneChance",
            "prestige", "cloneMult", "DUMMY_cloneCountPlural", "GreenCloneRewardBase", "activateNextBreakerLights",
            "enableCloneDustGeneration", "moreWatts", "baseRP" };
        static readonly string[] BoxGlobal = { "cashPerLoop", "fastCloneChance", "maxCloneFastness", "bigCloneChance", "maxCloneBigness",
            "cloneMult", "spawnNewAtom", "atomLevelChance", "greenCloneClance", "treeGrowth", "unlockPrestige", "openGate",
            "increasedWatts", "increasedGreenPower", "increasedNuclearPower", "area2DoorOpen", "zipMoversUnlocked",
            "exemptCourseFromAtomPrestige", "tripleThreatIncrease", "unlockJiggleDrops", "additionalCDdigits", "compBoostUnlocked",
            "compBoostTime", "compBoostStrength", "IncreasedCloneDust", "moreRefreshOrbs", "unlockTeleporters", "makeCourseBuyMax",
            "redCloneChance", "blueCloneChance", "NpCapIsSoftcap", "compBoostDoublesNP", "vmanTime", "startVmanSpeedrun", "moreWatts",
            "moreGP", "increasedRP", "moreRP", "RPcap", "GPAtomsDontResetCourses", "NpGenerationSpeed", "VmanStartsWithOmniDash",
            "wattPower", "MoreRpCap" };
        static readonly string[] BoxMovement = { "dash", "wallJump", "doubleJump", "swapBlocksOnce", "unlockBlockSwap", "endDemo", "OmniDash", "freeVmanOmnidash" };

        static readonly HashSet<string> BoxBoosts = new HashSet<string> { "cloneCount", "cashPerLoop", "fastCloneChance", "bigCloneChance",
            "cloneMult", "DUMMY_cloneCountPlural", "GreenCloneRewardBase", "moreWatts", "baseRP", "maxCloneFastness", "maxCloneBigness",
            "spawnNewAtom", "atomLevelChance", "greenCloneClance", "increasedWatts", "increasedGreenPower", "increasedNuclearPower",
            "tripleThreatIncrease", "additionalCDdigits", "compBoostTime", "compBoostStrength", "IncreasedCloneDust", "moreRefreshOrbs",
            "redCloneChance", "blueCloneChance", "compBoostDoublesNP", "vmanTime", "moreGP", "increasedRP", "moreRP", "RPcap",
            "NpGenerationSpeed", "wattPower", "MoreRpCap" };

        internal sealed record BoxInfo(string upgrade, string gives, double cost, string currency, int cap, string category,
            bool fresh, bool secret, bool tree,
            [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string number = null,
            [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string ach = null);

        static BoxInfo BuyInfo(JsonElement c, JsonElement o, bool tree)
        {
            var f = Fields(c);
            int pick(string name, int fallback = 0) => f.TryGetValue(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : fallback;
            string local = BoxLocal[Math.Clamp(pick("upgrade"), 0, BoxLocal.Length - 1)];
            string tail = local == "GLOBAL" ? BoxGlobal[Math.Clamp(pick("globalUpgrade"), 0, BoxGlobal.Length - 1)]
                : local == "Movement" ? BoxMovement[Math.Clamp(pick("movementUpgrade"), 0, BoxMovement.Length - 1)] : local;
            string upgrade = (local == "GLOBAL" ? "GLOBAL:" : local == "Movement" ? "Movement:" : "") + tail;
            string gives = Words(tail) + (local == "GLOBAL" ? " (global)" : local == "Movement" ? " (movement ability)" : " (local)");
            double cost = f.TryGetValue("baseUpgradeCost", out JsonElement bc) && bc.ValueKind == JsonValueKind.Number ? bc.GetDouble() : 0;
            string currency = Currencies[Math.Clamp(pick("upgradeCurrency"), 0, Currencies.Length - 1)];
            int cap = Math.Max(1, pick("Cap", 1));
            bool secret = f.TryGetValue("isSecret", out JsonElement s) && s.ValueKind == JsonValueKind.True;
            string achievement = f.TryGetValue("achievement", out JsonElement a) && a.ValueKind == JsonValueKind.String ? a.GetString() : "";
            string number = achievement.Length > 0 && achievement.All(char.IsDigit) ? achievement : null;
            string ach = achievement.StartsWith("ACH_") ? achievement : null;
            string category = secret ? "secret"
                : local == "Movement" ? "movement"
                : tree ? "tree"
                : cost <= 0 && cap == 1 ? (BoxBoosts.Contains(tail) ? "bonusBoost" : "bonusUnique")
                : ach != null ? "achievement"
                : cap == 1 ? "unlock" : "course";
            return new BoxInfo(upgrade, gives, cost, currency, cap, category,
                o.GetProperty("activeInHierarchy").GetBoolean(), secret, tree, number, ach);
        }

        static bool TreeOf(JsonElement c, JsonElement o, HashSet<int> trees, Dictionary<int, int> parents)
        {
            if (Fields(c).TryGetValue("upgradeTree", out JsonElement t) && t.ValueKind == JsonValueKind.Object) return true;
            for (int id = o.GetProperty("id").GetInt32(); id != 0 && parents.TryGetValue(id, out int p); id = p) if (trees.Contains(id)) return true;
            return false;
        }

        static string DisplayName(Dictionary<string, JsonElement> f, Dictionary<int, JsonElement> components)
        {
            return f.TryGetValue("upgradeNameDisplay", out JsonElement v) && v.ValueKind == JsonValueKind.Object && v.TryGetProperty("$ref", out JsonElement id)
                && components.TryGetValue(id.GetInt32(), out JsonElement t) && Fields(t).TryGetValue("m_text", out JsonElement text)
                && text.ValueKind == JsonValueKind.String && text.GetString().Trim() is { Length: > 0 } shown ? shown : null;
        }

        static readonly HashSet<string> Initialisms = new HashSet<string> { "W", "GP", "NP", "CD", "RP", "BP" };

        static string Words(string name)
        {
            var words = new List<string>();
            string token = "";
            foreach (char ch in name + "_")
            {
                if (ch == '_' || ch == ' ') { if (token.Length > 0) { words.Add(token); token = ""; } continue; }
                if (char.IsUpper(ch))
                {
                    if (token.Length > 0 && !char.IsUpper(token[token.Length - 1])) { words.Add(token); token = ""; }
                    token += ch;
                    continue;
                }
                if (token.Length > 1 && token.All(char.IsUpper))
                {
                    string prefix = Initialisms.Contains(token) ? token
                        : Initialisms.FirstOrDefault(i => token.StartsWith(i, StringComparison.Ordinal)) ?? token.Substring(0, token.Length - 1);
                    words.Add(prefix);
                    token = token.Substring(prefix.Length);
                }
                token += ch;
            }
            return string.Join(" ", words.Select(w => Initialisms.Contains(w.ToUpperInvariant()) ? w.ToUpperInvariant() : w.ToLowerInvariant()));
        }

        internal static List<Marker> Markers(JsonElement world, Vector3 origin)
        {
            var kinds = new Dictionary<string, string>
            {
                ["startGate"] = "start",
                ["endGate"] = "end",
                ["checkpointScript"] = "checkpoint",
                ["TeleporterScript"] = "teleporter",
                ["FakeCreditsControlScript"] = "falseEnding",
                ["EndCreditsTrigger"] = "trueEnding",
                ["JiggleDropScript"] = "refill",
                ["upgradeBox"] = "box"
            };
            var markers = new List<Marker>();
            HashSet<int> on = OnObjects(world, allStates: true);
            Dictionary<int, string> when = StateOf(world);
            var bodies = Bodies(world, origin);
            var components = new Dictionary<int, JsonElement>();
            var owners = new Dictionary<int, int>();
            var parents = new Dictionary<int, int>();
            var boxes = new HashSet<int>();
            foreach (JsonElement o in world.GetProperty("objects").EnumerateArray())
            {
                int id = o.GetProperty("id").GetInt32();
                parents[id] = o.GetProperty("parent").GetInt32();
                foreach (JsonElement c in o.GetProperty("components").EnumerateArray())
                {
                    components[c.GetProperty("id").GetInt32()] = c;
                    owners[c.GetProperty("id").GetInt32()] = id;
                    if (c.GetProperty("type").GetString() == "upgradeBox") boxes.Add(id);
                }
            }
            var trees = new HashSet<int>();
            foreach (JsonElement c in components.Values)
            {
                if (c.GetProperty("type").GetString() != "TreeController") continue;
                trees.Add(owners[c.GetProperty("id").GetInt32()]);
                if (Fields(c).TryGetValue("TreeSegments", out JsonElement list) && list.ValueKind == JsonValueKind.Array)
                    foreach (JsonElement r in list.EnumerateArray())
                        if (r.ValueKind == JsonValueKind.Object && r.TryGetProperty("$ref", out JsonElement id) && owners.TryGetValue(id.GetInt32(), out int segment)) trees.Add(segment);
            }
            var box = PlayerBox(world);
            foreach (JsonElement o in world.GetProperty("objects").EnumerateArray())
            {
                int id = o.GetProperty("id").GetInt32();
                if (!on.Contains(id) && !boxes.Contains(id)) continue;
                foreach (JsonElement c in o.GetProperty("components").EnumerateArray())
                {
                    if (!kinds.TryGetValue(c.GetProperty("type").GetString(), out string kind)) continue;
                    string name = o.GetProperty("name").GetString();
                    JsonElement p = o.GetProperty("position");
                    float x = Round(p.GetProperty("x").GetSingle() - origin.x), y = Round(p.GetProperty("y").GetSingle() - origin.y);
                    float[][] area = kind is "checkpoint" or "start" or "end" or "falseEnding" or "trueEnding" ? TriggerArea(o, bodies, origin, triggersOnly: false) : null;
                    float[] spawn = null;
                    BoxInfo info = null;
                    if (kind == "checkpoint" && box != null)
                    {
                        float cy = Round(y - 12f);
                        spawn = new[] { Round(x + box.Value.ox - box.Value.hx), Round(cy + box.Value.oy - box.Value.hy),
                            Round(x + box.Value.ox + box.Value.hx), Round(cy + box.Value.oy + box.Value.hy) };
                        y = cy;
                    }
                    if (kind == "refill") kind = name.Contains("Full") ? "fullRefill" : name.Contains("Jump") ? "jumpRefill" : "dashRefill";
                    else if (kind == "end" && Fields(c).TryGetValue("isEndOfCourse", out JsonElement eoc) && eoc.ValueKind == JsonValueKind.False) kind = "exit";
                    else if (kind == "box")
                    {
                        info = BuyInfo(c, o, TreeOf(c, o, trees, parents));
                        name = DisplayName(Fields(c), components) ?? name;
                        area = TriggerArea(o, bodies, origin, triggersOnly: true);
                        if (area.Length > 0)
                        {
                            float[] b = area[0];
                            x = Round((b.Where((_, i) => i % 2 == 0).Min() + b.Where((_, i) => i % 2 == 0).Max()) / 2f);
                            y = Round((b.Where((_, i) => i % 2 == 1).Min() + b.Where((_, i) => i % 2 == 1).Max()) / 2f);
                        }
                    }
                    markers.Add(new Marker(kind, name, x, y, area, when.TryGetValue(o.GetProperty("id").GetInt32(), out string state) ? state : null, spawn, info));
                    break;
                }
            }
            return markers;
        }

        internal sealed record Zip(string name, bool zip, float[] box, float[][] path, float[][] corridor,
            [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string when = null);

        internal static List<Zip> Zips(JsonElement world, Vector3 origin)
        {
            var zips = new List<Zip>();
            HashSet<int> on = OnObjects(world, allStates: true);
            Dictionary<int, string> when = StateOf(world);
            var bodies = Bodies(world, origin);
            var byId = world.GetProperty("objects").EnumerateArray().ToDictionary(o => o.GetProperty("id").GetInt32());
            foreach (JsonElement o in byId.Values)
            {
                int oid = o.GetProperty("id").GetInt32();
                if (!on.Contains(oid)) continue;
                foreach (JsonElement c in o.GetProperty("components").EnumerateArray())
                {
                    if (c.GetProperty("type").GetString() != "PlatformMover") continue;
                    var f = Fields(c);
                    if (!f.TryGetValue("Positions", out JsonElement positions) || positions.ValueKind != JsonValueKind.Array) break;
                    var local = positions.EnumerateArray().Select(p => p.GetProperty("position")).Select(p => (x: p.GetProperty("x").GetSingle(), y: p.GetProperty("y").GetSingle())).ToList();
                    if (local.Count < 2) break;
                    int at = f.TryGetValue("indexInPositions", out JsonElement ix) && ix.ValueKind == JsonValueKind.Number ? Math.Clamp(ix.GetInt32(), 0, local.Count - 1) : 0;
                    float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                    foreach (float[] poly in TriggerArea(o, bodies, origin, triggersOnly: false))
                        for (int i = 0; i + 1 < poly.Length; i += 2) { x0 = Math.Min(x0, poly[i]); x1 = Math.Max(x1, poly[i]); y0 = Math.Min(y0, poly[i + 1]); y1 = Math.Max(y1, poly[i + 1]); }
                    if (x0 > x1) break;
                    JsonElement lp = o.GetProperty("localPosition"), ls = o.GetProperty("localScale"), gs = o.GetProperty("lossyScale");
                    float sx = gs.GetProperty("x").GetSingle() / Math.Max(1e-6f, ls.GetProperty("x").GetSingle()), sy = gs.GetProperty("y").GetSingle() / Math.Max(1e-6f, ls.GetProperty("y").GetSingle());
                    float nowX = lp.GetProperty("x").GetSingle(), nowY = lp.GetProperty("y").GetSingle();
                    var path = new List<float[]>();
                    var corridor = new List<float[]>();
                    float[] BoxAt((float x, float y) p) => new[] { x0 + (p.x - nowX) * sx, y0 + (p.y - nowY) * sy, x1 + (p.x - nowX) * sx, y1 + (p.y - nowY) * sy };
                    for (int k = 0; k <= local.Count; k++)
                    {
                        var p = local[(at + k) % local.Count];
                        float[] b = BoxAt(p);
                        path.Add(new[] { Round((b[0] + b[2]) / 2), Round((b[1] + b[3]) / 2) });
                        if (k == 0) continue;
                        float[] a = BoxAt(local[(at + k - 1) % local.Count]);
                        corridor.Add(Hull(new[] { (a[0], a[1]), (a[2], a[1]), (a[2], a[3]), (a[0], a[3]), (b[0], b[1]), (b[2], b[1]), (b[2], b[3]), (b[0], b[3]) }));
                    }
                    bool zip = f.TryGetValue("PlatformType", out JsonElement pt) && pt.ValueKind == JsonValueKind.Number && pt.GetInt32() == 1;
                    zips.Add(new Zip(o.GetProperty("parent").GetInt32() is int parent && byId.TryGetValue(parent, out JsonElement po) ? po.GetProperty("name").GetString() : o.GetProperty("name").GetString(),
                        zip, new[] { Round(x0), Round(y0), Round(x1), Round(y1) }, path.ToArray(), corridor.ToArray(), when.TryGetValue(oid, out string w) ? w : null));
                    break;
                }
            }
            return zips;
        }

        static float[] Hull((float x, float y)[] pts)
        {
            var p = pts.Distinct().OrderBy(q => q.x).ThenBy(q => q.y).ToArray();
            if (p.Length < 3) return p.SelectMany(q => new[] { Round(q.x), Round(q.y) }).ToArray();
            static float Cross((float x, float y) o, (float x, float y) a, (float x, float y) b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
            var h = new List<(float x, float y)>();
            for (int pass = 0; pass < 2; pass++)
            {
                int start = h.Count;
                foreach (var q in pass == 0 ? p : p.Reverse())
                {
                    while (h.Count >= start + 2 && Cross(h[h.Count - 2], h[h.Count - 1], q) <= 0) h.RemoveAt(h.Count - 1);
                    h.Add(q);
                }
                h.RemoveAt(h.Count - 1);
            }
            return h.SelectMany(q => new[] { Round(q.x), Round(q.y) }).ToArray();
        }

        static Dictionary<string, JsonElement> Fields(JsonElement component)
        {
            var fields = new Dictionary<string, JsonElement>();
            if (component.TryGetProperty("fields", out JsonElement list))
                foreach (JsonElement f in list.EnumerateArray()) fields[f[1].GetString()] = f[2];
            return fields;
        }

        static (float hx, float hy, float ox, float oy)? PlayerBox(JsonElement world)
        {
            foreach (JsonElement o in world.GetProperty("objects").EnumerateArray())
                foreach (JsonElement c in o.GetProperty("components").EnumerateArray())
                {
                    if (c.GetProperty("type").GetString() != "Movement") continue;
                    var f = Fields(c);
                    if (!f.TryGetValue("defaultColliderSize", out JsonElement size) || !f.TryGetValue("defaultColliderOffset", out JsonElement offset)) return null;
                    return (size.GetProperty("x").GetSingle() / 2f, size.GetProperty("y").GetSingle() / 2f,
                        offset.GetProperty("x").GetSingle(), offset.GetProperty("y").GetSingle());
                }
            return null;
        }

        static float[][] TriggerArea(JsonElement o, Dictionary<int, (float x, float y, float cos, float sin, bool moving)> bodies, Vector3 origin, bool triggersOnly)
        {
            var area = new List<float[]>();
            foreach (JsonElement c in o.GetProperty("components").EnumerateArray())
            {
                if (!c.TryGetProperty("shapes", out JsonElement shapes) || shapes.ValueKind != JsonValueKind.Object) continue;
                if (c.TryGetProperty("enabled", out JsonElement enabled) && !enabled.GetBoolean()) continue;
                if (triggersOnly && !(c.GetProperty("props").TryGetProperty("isTrigger", out JsonElement t) && t.GetBoolean())) continue;
                var body = BodyOf(c.GetProperty("props"), bodies);
                foreach (JsonElement s in shapes.GetProperty("list").EnumerateArray())
                {
                    if (s.GetProperty("type").GetString() != "Polygon") continue;
                    float[] v = s.GetProperty("vertices").EnumerateArray().Select(e => e.GetSingle()).ToArray();
                    if (v.Length < 6) continue;
                    ToWorld(v, body, origin);
                    area.Add(v.Select(Round).ToArray());
                }
            }
            return area.ToArray();
        }
    }
}
