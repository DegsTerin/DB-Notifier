import math
import os
import sys
import threading
import tkinter as tk
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageEnhance, ImageOps, ImageTk
import pystray
from pystray import Menu, MenuItem


POPUP_W = 344
POPUP_H = 385

PANEL_TOP = "#101d2a"
PANEL_BOTTOM = "#07121d"
PANEL_BORDER = "#5a6b7a"
RULE = "#40505f"
TEXT = "#ffffff"
MUTED = "#c8cdd5"
GREEN = "#72ff75"
YELLOW = "#ffd24b"
RED = "#ff6758"
HOVER = "#1b3348"

BASE_DIR = Path(__file__).resolve().parent
ASSETS_DIR = BASE_DIR / "assets"
POSTGRES_ICON = ASSETS_DIR / "postgres.png"


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
    (15, 232, 329, 258, "Restart service"),
    (15, 260, 329, 286, "Open log"),
    (15, 288, 329, 314, "Open config"),
    (15, 316, 329, 342, "Reload configuration"),
    (15, 358, 329, 383, "Exit"),
]


def resource_path(path: Path) -> Path:
    if hasattr(sys, "_MEIPASS"):
        return Path(sys._MEIPASS) / path.relative_to(BASE_DIR)
    return path


def hex_to_rgb(value: str) -> tuple[int, int, int]:
    value = value.lstrip("#")
    return tuple(int(value[i : i + 2], 16) for i in (0, 2, 4))


def rgb_to_hex(rgb: tuple[int, int, int]) -> str:
    return "#{:02x}{:02x}{:02x}".format(*rgb)


def blend(a: str, b: str, t: float) -> str:
    ar, ag, ab = hex_to_rgb(a)
    br, bg, bb = hex_to_rgb(b)
    return rgb_to_hex((int(ar + (br - ar) * t), int(ag + (bg - ag) * t), int(ab + (bb - ab) * t)))


def rounded_rect(canvas: tk.Canvas, x1: int, y1: int, x2: int, y2: int, radius: int, **kwargs):
    points = [
        x1 + radius,
        y1,
        x2 - radius,
        y1,
        x2,
        y1,
        x2,
        y1 + radius,
        x2,
        y2 - radius,
        x2,
        y2,
        x2 - radius,
        y2,
        x1 + radius,
        y2,
        x1,
        y2,
        x1,
        y2 - radius,
        x1,
        y1 + radius,
        x1,
        y1,
    ]
    return canvas.create_polygon(points, smooth=True, splinesteps=18, **kwargs)


def draw_vertical_gradient(canvas: tk.Canvas, x1: int, y1: int, x2: int, y2: int, top: str, bottom: str):
    height = max(1, y2 - y1)
    for y in range(y1, y2):
        canvas.create_line(x1, y, x2, y, fill=blend(top, bottom, (y - y1) / height))


def load_official_icon(size: int, green: bool = True) -> Image.Image:
    source = resource_path(POSTGRES_ICON)
    if not source.exists():
        raise FileNotFoundError(f"PostgreSQL icon not found: {source}")

    image = Image.open(source).convert("RGBA")
    image.thumbnail((size, size), Image.LANCZOS)

    if not green:
        canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        canvas.alpha_composite(image, ((size - image.width) // 2, (size - image.height) // 2))
        return canvas

    alpha = image.getchannel("A")
    grey = ImageOps.grayscale(image)
    tinted = ImageOps.colorize(grey, black="#0a2617", white=GREEN).convert("RGBA")
    tinted.putalpha(alpha)
    tinted = ImageEnhance.Contrast(tinted).enhance(1.18)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.alpha_composite(tinted, ((size - tinted.width) // 2, (size - tinted.height) // 2))
    return canvas


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
        angle = math.radians(i * 45)
        canvas.create_line(cx + math.cos(angle) * 5, cy + math.sin(angle) * 5, cx + math.cos(angle) * 8, cy + math.sin(angle) * 8, fill=color, width=1.5)
    canvas.create_oval(cx - 5, cy - 5, cx + 5, cy + 5, outline=color, width=1.5)
    canvas.create_oval(cx - 1.5, cy - 1.5, cx + 1.5, cy + 1.5, fill=color, outline=color)


def icon_exit(canvas: tk.Canvas, x: int, y: int, color: str = TEXT):
    canvas.create_rectangle(x + 3, y + 3, x + 13, y + 16, outline=color, width=1.2)
    canvas.create_line(x + 9, y + 10, x + 18, y + 10, fill=color, width=1.5)
    canvas.create_line(x + 15, y + 7, x + 18, y + 10, fill=color, width=1.5)
    canvas.create_line(x + 15, y + 13, x + 18, y + 10, fill=color, width=1.5)


class PgNotifierTrayApp:
    def __init__(self):
        self.root = tk.Tk()
        self.root.withdraw()
        self.root.title("PgNotifier")

        self.popup: tk.Toplevel | None = None
        self.canvas: tk.Canvas | None = None
        self.hover_index: int | None = None
        self.header_icon = ImageTk.PhotoImage(load_official_icon(30, green=True))
        self.tray_icon_image = load_official_icon(64, green=True)

        self.tray_icon = pystray.Icon(
            "PgNotifier",
            self.tray_icon_image,
            "PgNotifier",
            menu=Menu(
                MenuItem("Show", self._tray_show, default=True, visible=False),
                MenuItem("Exit", self._tray_exit),
            ),
        )

    def run(self):
        threading.Thread(target=self.tray_icon.run, daemon=True).start()
        self.root.mainloop()

    def _tray_show(self, _icon=None, _item=None):
        self.root.after(0, self.toggle_popup)

    def _tray_exit(self, _icon=None, _item=None):
        self.root.after(0, self.exit)

    def exit(self):
        try:
            self.tray_icon.stop()
        finally:
            self.root.destroy()

    def toggle_popup(self):
        if self.popup and self.popup.winfo_exists() and self.popup.state() == "normal":
            self.hide_popup()
            return
        self.show_popup()

    def show_popup(self):
        if self.popup and self.popup.winfo_exists():
            self.popup.destroy()

        self.popup = tk.Toplevel(self.root)
        self.popup.withdraw()
        self.popup.overrideredirect(True)
        self.popup.attributes("-topmost", True)
        self.popup.configure(bg=PANEL_BORDER)

        screen_w = self.popup.winfo_screenwidth()
        screen_h = self.popup.winfo_screenheight()
        x = max(8, screen_w - POPUP_W - 36)
        y = max(8, screen_h - POPUP_H - 118)
        self.popup.geometry(f"{POPUP_W}x{POPUP_H}+{x}+{y}")

        self.canvas = tk.Canvas(self.popup, width=POPUP_W, height=POPUP_H, highlightthickness=0, bd=0, bg=PANEL_BORDER)
        self.canvas.pack(fill="both", expand=True)
        self.canvas.bind("<Motion>", self.on_motion)
        self.canvas.bind("<Leave>", self.on_leave)
        self.canvas.bind("<Button-1>", self.on_click)
        self.popup.bind("<FocusOut>", lambda _event: self.hide_popup())
        self.popup.bind("<Escape>", lambda _event: self.hide_popup())

        self.draw()
        self.popup.deiconify()
        self.popup.focus_force()

    def hide_popup(self):
        if self.popup and self.popup.winfo_exists():
            self.popup.withdraw()

    def on_motion(self, event):
        next_hover = None
        for index, (x1, y1, x2, y2, _label) in enumerate(ACTION_ROWS):
            if x1 <= event.x <= x2 and y1 <= event.y <= y2:
                next_hover = index
                break
        if next_hover != self.hover_index:
            self.hover_index = next_hover
            self.draw()

    def on_leave(self, _event):
        if self.hover_index is not None:
            self.hover_index = None
            self.draw()

    def on_click(self, event):
        for _index, (x1, y1, x2, y2, label) in enumerate(ACTION_ROWS):
            if x1 <= event.x <= x2 and y1 <= event.y <= y2:
                if label == "Exit":
                    self.exit()
                return

    def draw(self):
        canvas = self.canvas
        if canvas is None:
            return

        canvas.delete("all")
        rounded_rect(canvas, 0, 0, POPUP_W - 1, POPUP_H - 1, 9, fill=PANEL_BORDER, outline="")
        rounded_rect(canvas, 1, 1, POPUP_W - 2, POPUP_H - 2, 8, fill=PANEL_TOP, outline="")
        draw_vertical_gradient(canvas, 2, 2, POPUP_W - 3, POPUP_H - 3, PANEL_TOP, PANEL_BOTTOM)
        rounded_rect(canvas, 1, 1, POPUP_W - 2, POPUP_H - 2, 8, outline=PANEL_BORDER, fill="", width=1)

        self.draw_header(canvas)
        self.draw_instances(canvas)
        self.draw_actions(canvas)

    def draw_header(self, canvas: tk.Canvas):
        canvas.create_image(23, 15, anchor="nw", image=self.header_icon)
        canvas.create_text(62, 27, anchor="w", text="All instances are healthy", fill=GREEN, font=("Segoe UI", 10, "normal"))
        canvas.create_line(15, 47, POPUP_W - 16, 47, fill=RULE)

    def draw_instances(self, canvas: tk.Canvas):
        y_positions = [70, 123, 176]
        for instance, y in zip(INSTANCES, y_positions):
            canvas.create_oval(26, y + 1, 39, y + 14, fill=instance.color, outline="")
            canvas.create_text(52, y, anchor="nw", text=instance.name, fill=TEXT, font=("Segoe UI", 10, "normal"))
            canvas.create_text(52, y + 22, anchor="nw", text=instance.service, fill=MUTED, font=("Segoe UI", 9, "normal"))
            canvas.create_text(276, y, anchor="nw", text=instance.status, fill=instance.color, font=("Segoe UI", 10, "normal"))
            canvas.create_text(276, y + 22, anchor="nw", text=f"PID: {instance.pid}", fill=MUTED, font=("Segoe UI", 9, "normal"))

    def draw_actions(self, canvas: tk.Canvas):
        canvas.create_line(15, 222, POPUP_W - 16, 222, fill=RULE)
        actions = [
            ("Restart service", icon_restart, 232),
            ("Open log", icon_doc, 260),
            ("Open config", icon_gear, 288),
            ("Reload configuration", icon_restart, 316),
        ]
        for index, (label, icon, y) in enumerate(actions):
            self.draw_action(canvas, index, y, label, icon)
        canvas.create_line(15, 356, POPUP_W - 16, 356, fill=RULE)
        self.draw_action(canvas, 4, 361, "Exit", icon_exit)

    def draw_action(self, canvas: tk.Canvas, index: int, y: int, label: str, icon):
        if self.hover_index == index:
            rounded_rect(canvas, 17, y - 2, POPUP_W - 18, y + 26, 4, fill=HOVER, outline="")
        icon(canvas, 24, y + 3)
        canvas.create_text(53, y + 12, anchor="w", text=label, fill=TEXT, font=("Segoe UI", 10, "normal"))


def main():
    app = PgNotifierTrayApp()
    app.run()


if __name__ == "__main__":
    main()
