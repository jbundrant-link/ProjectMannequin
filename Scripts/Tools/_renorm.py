from PIL import Image

M = "Assets/Sprites/Mannequin/mannequin_sheet_higgsfield_v1.png"
FS = 256
BASE = 248
TARGET_HEAD = 26.0

m = Image.open(M).convert("RGBA")


def head_width(img):
    a = img.getchannel("A")
    bb = a.getbbox()
    if not bb:
        return 1
    ld = a.load()
    top, bot = bb[1], bb[3]
    h = bot - top
    ws = []
    for y in range(int(top + 0.04 * h), int(top + 0.16 * h) + 1):
        xs = [x for x in range(bb[0], bb[2]) if ld[x, y] > 60]
        if xs:
            ws.append(max(xs) - min(xs))
    return sorted(ws)[len(ws) // 2] if ws else 1


def renorm(row, count):
    for c in range(count):
        cell = m.crop((c * FS, row * FS, c * FS + FS, row * FS + FS))
        bb = cell.getchannel("A").getbbox()
        if not bb:
            continue
        content = cell.crop(bb)
        hw = head_width(content)
        s = TARGET_HEAD / max(1, hw)
        w, h = content.size
        s = min(s, 244.0 / w, 250.0 / h)
        r2 = content.resize((max(1, round(w * s)), max(1, round(h * s))), Image.LANCZOS)
        m.paste((0, 0, 0, 0), (c * FS, row * FS, c * FS + FS, row * FS + FS))
        x = (FS - r2.width) // 2
        y = max(0, BASE - r2.height)
        m.alpha_composite(r2, (c * FS + x, row * FS + y))


# rows 1 (walk, 8) and 2 (dash, 6); cols 8/6 used, but renorm all 10 harmlessly
renorm(1, 10)
renorm(2, 10)
m.save(M)


def hw_h(row, n):
    out = []
    for c in range(n):
        cell = m.crop((c * FS, row * FS, c * FS + FS, row * FS + FS))
        bb = cell.getchannel("A").getbbox()
        out.append((head_width(cell.crop(bb)) if bb else 0, (bb[3] - bb[1]) if bb else 0))
    return out


print("IDLE", hw_h(0, 4))
print("WALK", hw_h(1, 8))
print("DASH", hw_h(2, 6))
print("saved")
