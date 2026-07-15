"""Dev helper for regenerating mannequin walk/dash from green-screen gpt sheets.

Usage:
  python Scripts/Tools/_sprite_dev.py analyze <sheet.png> <cols> <rows>
  python Scripts/Tools/_sprite_dev.py preview <sheet.png> <out.png> <cols> <rows>
  python Scripts/Tools/_sprite_dev.py build <src.png> <cols> <rows> <out.png> <order-csv>
      builds a reordered green-screen source sheet (order indices into the cols*rows grid)
"""
import sys
from PIL import Image, ImageDraw


def is_green(r, g, b):
    return g > 105 and g > r + 30 and g > b + 30


def cells(path, cols, rows):
    im = Image.open(path).convert("RGBA")
    W, H = im.size
    cw, ch = W // cols, H // rows
    out = []
    for r in range(rows):
        for c in range(cols):
            out.append(im.crop((c * cw, r * ch, c * cw + cw, r * ch + ch)))
    return out, (cw, ch)


def lead(cell):
    px = cell.load()
    w, h = cell.size
    pts = [(x, y) for y in range(h) for x in range(w) if not is_green(*px[x, y][:3])]
    if not pts:
        return "empty", 0, 0
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    mnx, mxx, mny, mxy = min(xs), max(xs), min(ys), max(ys)
    cx = (mnx + mxx) / 2
    hh = mxy - mny
    legs = [(x, y) for x, y in pts if y > mny + hh * 0.62]
    fwd = [sum(px[x, y][:3]) / 3 for x, y in legs if x > cx + 20]
    bak = [sum(px[x, y][:3]) / 3 for x, y in legs if x < cx - 20]
    fb = sum(fwd) / len(fwd) if fwd else 0
    bk = sum(bak) / len(bak) if bak else 0
    tag = "FAR/dark" if fb < bk - 8 else ("NEAR/light" if fb > bk + 8 else "even")
    return tag, hh, fb - bk


def cmd_analyze(path, cols, rows):
    cl, _ = cells(path, cols, rows)
    for i, c in enumerate(cl):
        tag, hh, diff = lead(c)
        print("f%d: H=%d diff=%+.0f -> %s" % (i, hh, diff, tag))


def checker(w, h, st=13):
    b = Image.new("RGBA", (w, h), (58, 58, 66, 255))
    d = ImageDraw.Draw(b)
    for y in range(0, h, st):
        for x in range(0, w, st):
            if (x // st + y // st) % 2 == 0:
                d.rectangle((x, y, x + st, y + st), fill=(82, 82, 90, 255))
    return b


def cmd_preview(path, out, cols, rows):
    im = Image.open(path).convert("RGBA")
    W, H = im.size
    px = im.load()
    keyed = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    kp = keyed.load()
    for y in range(H):
        for x in range(W):
            r, g, b, a = px[x, y]
            if not is_green(r, g, b):
                kp[x, y] = (r, g, b, 255)
    sc = 1000 / W
    prev = keyed.resize((1000, int(H * sc)), Image.LANCZOS)
    bg = checker(prev.width, prev.height)
    bg.alpha_composite(prev)
    bg.convert("RGB").save(out)
    print("preview", out)


def cmd_build(src, cols, rows, out, order_csv):
    cl, (cw, ch) = cells(src, cols, rows)
    order = [int(x) for x in order_csv.split(",")]
    # output sheet keeps same cell grid dimensions
    sheet = Image.new("RGBA", (cw * cols, ch * rows), (0, 255, 0, 255))
    for pos, idx in enumerate(order):
        r, c = divmod(pos, cols)
        sheet.paste(cl[idx], (c * cw, r * ch))
    sheet.convert("RGB").save(out)
    print("built", out, sheet.size, "order", order)


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "analyze":
        cmd_analyze(sys.argv[2], int(sys.argv[3]), int(sys.argv[4]))
    elif cmd == "preview":
        cmd_preview(sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5]))
    elif cmd == "build":
        cmd_build(sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5], sys.argv[6])
