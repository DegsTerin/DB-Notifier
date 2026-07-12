"""Prototype a pixel-accurate DB-Notifier interface using deterministic demonstration data."""

import math
import tkinter as tk
from dataclasses import dataclass


WINDOW_W = 386
WINDOW_H = 453

BG_TOP = "#020713"
BG_MID = "#04111f"
BG_BOTTOM = "#001821"
PANEL_TOP = "#111f2d"
PANEL_BOTTOM = "#07121d"
PANEL_BORDER = "#637383"
RULE = "#465564"
TEXT = "#ffffff"
MUTED = "#c9ced6"
GREEN = "#72ff75"
YELLOW = "#ffd24b"
RED = "#ff6656"
TRAY = "#0c1926"
TRAY_ACTIVE = "#1c3447"


@dataclass
class Instance:
    name: str
    service: str
    status: str
    pid: str
    color: str


INSTANCES = [
    Instance("Local PostgreSQL 18", "postgresql-x64-18", "Running", "12345", GREEN),
    Instance("Analytics DB", "postgresql-analytics", "Restarted", "23456", YELLOW),
    Instance("Reporting DB", "postgresql-reporting", "Stopped", "-", RED),
]

ACTION_ROWS = [
    (24, 237, 336, 264),
    (24, 265, 336, 292),
    (24, 293, 336, 320),
    (24, 321, 336, 348),
    (24, 363, 336, 390),
]


def hex_to_rgb(value: str) -> tuple[int, int, int]:
    value = value.lstrip("#")
    return tuple(int(value[i : i + 2], 16) for i in (0, 2, 4))


def rgb_to_hex(rgb: tuple[int, int, int]) -> str:
    return "#{:02x}{:02x}{:02x}".format(*rgb)


def blend(a: str, b: str, t: float) -> str:
    ar, ag, ab = hex_to_rgb(a)
    br, bg, bb = hex_to_rgb(b)
    return rgb_to_hex(
        (
            int(ar + (br - ar) * t),
            int(ag + (bg - ag) * t),
            int(ab + (bb - ab) * t),
        )
    )


def rounded_rect(canvas: tk.Canvas, x1: int, y1: int, x2: int, y2: int, radius: int, **kwargs):
    points = [
        x1 + radius,
        y1,
        x2 - radius,
        y1,
        x2 - radius,
        y1,
        x2,
        y1,
        x2,
        y1 + radius,
        x2,
        y1 + radius,
        x2,
        y2 - radius,
        x2,
        y2 - radius,
        x2,
        y2,
        x2 - radius,
        y2,
        x2 - radius,
        y2,
        x1 + radius,
        y2,
        x1 + radius,
        y2,
        x1,
        y2,
        x1,
        y2 - radius,
        x1,
        y2 - radius,
        x1,
        y1 + radius,
        x1,
        y1 + radius,
        x1,
        y1,
        x1 + radius,
        y1,
    ]
    return canvas.create_polygon(points, smooth=True, splinesteps=18, **kwargs)


def draw_vertical_gradient(canvas: tk.Canvas, x1: int, y1: int, x2: int, y2: int, top: str, bottom: str):
    height = max(1, y2 - y1)
    for y in range(y1, y2):
        t = (y - y1) / height
        canvas.create_line(x1, y, x2, y, fill=blend(top, bottom, t))


def draw_background(canvas: tk.Canvas):
    draw_vertical_gradient(canvas, 0, 0, WINDOW_W, WINDOW_H, BG_TOP, BG_BOTTOM)
    canvas.create_oval(64, 296, 520, 626, outline="", fill="#001724")
    canvas.create_line(0, 330, 6, 330, fill="#18ff68", width=2)
    draw_vertical_gradient(canvas, 0, 392, WINDOW_W, WINDOW_H, "#071828", "#00070d")
    canvas.create_rectangle(0, 439, WINDOW_W, WINDOW_H, outline="", fill="#081320")
    canvas.create_line(0, 439, WINDOW_W, 439, fill="#253749")


def draw_postgres_logo(canvas: tk.Canvas, x: int, y: int, scale: float = 1.0):
    s = scale
    canvas.create_oval(x + 3 * s, y + 2 * s, x + 26 * s, y + 26 * s, fill=GREEN, outline="")
    canvas.create_arc(x + 7 * s, y + 7 * s, x + 16 * s, y + 21 * s, start=90, extent=230, outline="#eaffef", width=max(1, int(2 * s)), style=tk.ARC)
    canvas.create_arc(x + 14 * s, y + 7 * s, x + 24 * s, y + 21 * s, start=220, extent=230, outline="#eaffef", width=max(1, int(2 * s)), style=tk.ARC)
    canvas.create_line(x + 14 * s, y + 17 * s, x + 14 * s, y + 27 * s, fill="#eaffef", width=max(1, int(2 * s)))
    canvas.create_line(x + 14 * s, y + 27 * s, x + 20 * s, y + 24 * s, fill="#eaffef", width=max(1, int(2 * s)))
    canvas.create_text(x + 12 * s, y + 14 * s, text="P", fill="#08111a", font=("Segoe UI", max(7, int(8 * s)), "bold"))


def draw_header(canvas: tk.Canvas):
    draw_postgres_logo(canvas, 21, 21, 0.86)
    canvas.create_text(73, 35, anchor="w", text="All instances are healthy", fill=GREEN, font=("Segoe UI", 11, "normal"))
    canvas.create_line(24, 56, 336, 56, fill=RULE)


def draw_instance(canvas: tk.Canvas, instance: Instance, y: int):
    canvas.create_oval(27, y + 11, 39, y + 23, fill=instance.color, outline="")
    canvas.create_text(52, y + 9, anchor="w", text=instance.name, fill=TEXT, font=("Segoe UI", 10, "normal"))
    canvas.create_text(52, y + 31, anchor="w", text=instance.service, fill=MUTED, font=("Segoe UI", 9, "normal"))
    canvas.create_text(276, y + 9, anchor="w", text=instance.status, fill=instance.color, font=("Segoe UI", 10, "normal"))
    canvas.create_text(276, y + 31, anchor="w", text=f"PID: {instance.pid}", fill=MUTED, font=("Segoe UI", 9, "normal"))


def icon_restart(canvas: tk.Canvas, x: int, y: int, color: str = TEXT):
    canvas.create_arc(x, y, x + 17, y + 17, start=35, extent=290, outline=color, width=1.5, style=tk.ARC)
    canvas.create_line(x + 14, y + 1, x + 18, y + 1, fill=color, width=1.5)
    canvas.create_line(x + 14, y + 1, x + 14, y + 5, fill=color, width=1.5)


def icon_doc(canvas: tk.Canvas, x: int, y: int, color: str = TEXT):
    canvas.create_rectangle(x + 2, y + 1, x + 14, y + 17, outline=color, width=1.2)
    canvas.create_line(x + 10, y + 1, x + 14, y + 5, fill=color, width=1.2)
    canvas.create_line(x + 5, y + 8, x + 12, y + 8, fill=color)
    canvas.create_line(x + 5, y + 12, x + 12, y + 12, fill=color)


def icon_gear(canvas: tk.Canvas, x: int, y: int, color: str = TEXT):
    cx, cy = x + 8, y + 9
    for i in range(8):
        a = math.radians(i * 45)
        canvas.create_line(cx + math.cos(a) * 5, cy + math.sin(a) * 5, cx + math.cos(a) * 8, cy + math.sin(a) * 8, fill=color, width=1.5)
    canvas.create_oval(cx - 5, cy - 5, cx + 5, cy + 5, outline=color, width=1.5)
    canvas.create_oval(cx - 1.5, cy - 1.5, cx + 1.5, cy + 1.5, fill=color, outline=color)


def icon_exit(canvas: tk.Canvas, x: int, y: int, color: str = TEXT):
    canvas.create_rectangle(x + 3, y + 3, x + 13, y + 16, outline=color, width=1.2)
    canvas.create_line(x + 9, y + 10, x + 18, y + 10, fill=color, width=1.5)
    canvas.create_line(x + 15, y + 7, x + 18, y + 10, fill=color, width=1.5)
    canvas.create_line(x + 15, y + 13, x + 18, y + 10, fill=color, width=1.5)


def draw_action(canvas: tk.Canvas, y: int, text: str, icon, hovered: bool = False):
    tag = f"action-{text}"
    if hovered:
        rounded_rect(canvas, 22, y - 3, 338, y + 25, 5, fill="#1a3348", outline="", tags=(tag,))
    icon(canvas, 25, y + 2)
    canvas.create_text(53, y + 11, anchor="w", text=text, fill=TEXT, font=("Segoe UI", 10, "normal"), tags=(tag,))


def draw_panel(canvas: tk.Canvas, hover_index: int | None = None):
    rounded_rect(canvas, 8, 9, 351, 393, 10, fill=PANEL_BORDER, outline="")
    rounded_rect(canvas, 9, 10, 350, 392, 9, fill=PANEL_TOP, outline="")
    draw_vertical_gradient(canvas, 10, 11, 349, 391, PANEL_TOP, PANEL_BOTTOM)
    rounded_rect(canvas, 9, 10, 350, 392, 9, outline=PANEL_BORDER, fill="", width=1)
    draw_header(canvas)

    y_positions = [72, 126, 180]
    for instance, y in zip(INSTANCES, y_positions):
        draw_instance(canvas, instance, y)

    canvas.create_line(24, 231, 336, 231, fill=RULE)
    draw_action(canvas, 239, "Restart service", icon_restart, hover_index == 0)
    draw_action(canvas, 267, "Open log", icon_doc, hover_index == 1)
    draw_action(canvas, 295, "Open config", icon_gear, hover_index == 2)
    draw_action(canvas, 323, "Reload configuration", icon_restart, hover_index == 3)
    canvas.create_line(24, 357, 336, 357, fill=RULE)
    draw_action(canvas, 365, "Exit", icon_exit, hover_index == 4)


def draw_tray_footer(canvas: tk.Canvas):
    rounded_rect(canvas, 163, 406, 204, 445, 9, fill=TRAY_ACTIVE, outline="")
    draw_postgres_logo(canvas, 174, 415, 0.72)
    canvas.create_text(223, 424, text="⌃", fill=TEXT, font=("Segoe UI", 15, "normal"))
    canvas.create_rectangle(248, 417, 260, 430, outline=MUTED, width=1)
    canvas.create_line(246, 432, 262, 432, fill=MUTED)
    canvas.create_text(278, 424, text=")))", fill=TEXT, font=("Segoe UI", 11, "normal"))
    canvas.create_text(356, 417, anchor="e", text="10:15 AM", fill=TEXT, font=("Segoe UI", 9, "normal"))
    canvas.create_text(356, 435, anchor="e", text="4/19/2026", fill=TEXT, font=("Segoe UI", 9, "normal"))


def draw_static(canvas: tk.Canvas, hover_index: int | None = None):
    canvas.delete("all")
    draw_background(canvas)
    draw_panel(canvas, hover_index)
    draw_tray_footer(canvas)


def main():
    root = tk.Tk()
    root.title("DB-Notifier Pixel Prototype")
    root.geometry(f"{WINDOW_W}x{WINDOW_H}")
    root.resizable(False, False)
    root.configure(bg=BG_TOP)

    canvas = tk.Canvas(root, width=WINDOW_W, height=WINDOW_H, highlightthickness=0, bd=0, bg=BG_TOP)
    canvas.pack(fill="both", expand=True)
    draw_static(canvas)

    state = {"hover": None}

    def on_motion(event):
        next_hover = None
        for index, (x1, y1, x2, y2) in enumerate(ACTION_ROWS):
            if x1 <= event.x <= x2 and y1 <= event.y <= y2:
                next_hover = index
                break
        if next_hover != state["hover"]:
            state["hover"] = next_hover
            draw_static(canvas, next_hover)

    def on_leave(_event):
        if state["hover"] is not None:
            state["hover"] = None
            draw_static(canvas)

    canvas.bind("<Motion>", on_motion)
    canvas.bind("<Leave>", on_leave)
    canvas.bind("<Button-1>", lambda event: None)
    root.mainloop()


if __name__ == "__main__":
    main()
