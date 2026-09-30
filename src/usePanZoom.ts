import { useEffect, useRef, useState, type PointerEvent } from "react";
import type { P } from "./geometry";
import { zoomAt, type Size, type View } from "./view";

type Press = { x: number; y: number; sx: number; sy: number };

export type PanZoomEvents = {
  onTap: (p: P, reach: number, shiftKey: boolean) => void;
  onPointer: (p: P | null, hoverReach: number | null) => void;
};

export function usePanZoom({ onTap, onPointer }: PanZoomEvents) {
  const svg = useRef<SVGSVGElement>(null);
  const [view, setView] = useState<View | null>(null);
  const [size, setSize] = useState<Size>({ w: 1, h: 1 });
  const presses = useRef(new Map<number, Press>());
  const moved = useRef(false);

  useEffect(() => {
    const el = svg.current;
    if (!el) return;
    const observer = new ResizeObserver(() => setSize({ w: el.clientWidth || 1, h: el.clientHeight || 1 }));
    observer.observe(el);
    const onWheel = (e: WheelEvent) => {
      e.preventDefault();
      const rect = el.getBoundingClientRect();
      setView(
        (v) => v && zoomAt(v, e.clientX - rect.left, e.clientY - rect.top, Math.exp(e.deltaY * 0.0015), rect.width),
      );
    };
    el.addEventListener("wheel", onWheel, { passive: false });
    return () => {
      observer.disconnect();
      el.removeEventListener("wheel", onWheel);
    };
  }, []);

  const toWorld = (e: { clientX: number; clientY: number }): P => {
    const el = svg.current;
    if (!el || !view) return [0, 0];
    const rect = el.getBoundingClientRect(),
      s = rect.width / view.w;
    return [view.x + (e.clientX - rect.left) / s, -(view.y + (e.clientY - rect.top) / s)];
  };
  const reach = (pixels: number) => (view ? (pixels * view.w) / size.w : 0);

  const onPointerDown = (e: PointerEvent<SVGSVGElement>) => {
    e.currentTarget.setPointerCapture(e.pointerId);
    presses.current.set(e.pointerId, { x: e.clientX, y: e.clientY, sx: e.clientX, sy: e.clientY });
    moved.current = presses.current.size > 1;
  };

  const onPointerMove = (e: PointerEvent<SVGSVGElement>) => {
    const el = svg.current,
      prev = presses.current.get(e.pointerId);
    onPointer(toWorld(e), view && !prev && e.pointerType === "mouse" ? reach(10) : null);
    if (!view || !el || !prev) return;
    const now = { ...prev, x: e.clientX, y: e.clientY };
    if (Math.abs(now.x - now.sx) + Math.abs(now.y - now.sy) > (e.pointerType === "mouse" ? 2 : 8)) moved.current = true;
    const rect = el.getBoundingClientRect(),
      other = [...presses.current].find(([id]) => id !== e.pointerId)?.[1];
    presses.current.set(e.pointerId, now);
    if (!moved.current) return;
    const pan = (v: View, dx: number, dy: number) => ({
      ...v,
      x: v.x - (dx * v.w) / rect.width,
      y: v.y - (dy * v.w) / rect.width,
    });
    if (!other) {
      setView((v) => v && pan(v, now.x - prev.x, now.y - prev.y));
      return;
    }
    const mx = (now.x + other.x) / 2 - rect.left,
      my = (now.y + other.y) / 2 - rect.top;
    const pmx = (prev.x + other.x) / 2 - rect.left,
      pmy = (prev.y + other.y) / 2 - rect.top;
    const f =
      Math.hypot(prev.x - other.x, prev.y - other.y) / Math.max(1, Math.hypot(now.x - other.x, now.y - other.y));
    setView((v) => v && zoomAt(pan(v, mx - pmx, my - pmy), mx, my, f, rect.width));
  };

  const onPointerUp = (e: PointerEvent<SVGSVGElement>) => {
    const had = presses.current.delete(e.pointerId);
    if (!had || moved.current || presses.current.size || !view) return;
    onTap(toWorld(e), reach(e.pointerType === "mouse" ? 10 : 24), e.shiftKey);
  };

  const handlers = {
    onPointerDown,
    onPointerMove,
    onPointerUp,
    onPointerCancel: (e: PointerEvent<SVGSVGElement>) => void presses.current.delete(e.pointerId),
    onPointerLeave: () => onPointer(null, null),
  };

  return { svg, view, setView, size, handlers };
}
