from PIL import Image, ImageDraw

m = Image.open("Assets/Sprites/Mannequin/mannequin_sheet_higgsfield_v1.png").convert("RGBA")
FS = 256


def hw(img):
    a = img.getchannel("A")
    bb = a.getbbox()
    if not bb:
        return 0, 0
    ld = a.load()
    top, bot = bb[1], bb[3]
    h = bot - top
    ws = []
    for y in range(int(top + 0.04 * h), int(top + 0.16 * h) + 1):
        xs = [x for x in range(bb[0], bb[2]) if ld[x, y] > 60]
        if xs:
            ws.append(max(xs) - min(xs))
    return (sorted(ws)[len(ws) // 2] if ws else 0), h


for lb, r, n in [("IDLE", 0, 4), ("WALK", 1, 8), ("DASH", 2, 6)]:
    print(lb, [hw(m.crop((c * FS, r * FS, c * FS + FS, r * FS + FS))) for c in range(n)])

S = 132


def checker(w, h, st=12):
    b = Image.new("RGBA", (w, h), (56, 56, 64, 255))
    d = ImageDraw.Draw(b)
    for y in range(0, h, st):
        for x in range(0, w, st):
            if (x // st + y // st) % 2 == 0:
                d.rectangle((x, y, x + st, y + st), fill=(80, 80, 90, 255))
    return b


disp = [("idle", m.crop((0, 0, FS, FS)))]
disp += [("w%d" % i, m.crop((i * FS, FS, i * FS + FS, 2 * FS))) for i in range(8)]
disp += [("d%d" % i, m.crop((i * FS, 2 * FS, i * FS + FS, 3 * FS))) for i in range(6)]
cv = Image.new("RGBA", (len(disp) * S + 8, S + 22), (18, 18, 24, 255))
d = ImageDraw.Draw(cv)
for i, (lb, im) in enumerate(disp):
    bg = checker(S, S)
    bg.alpha_composite(im.resize((S, S), Image.LANCZOS))
    cv.alpha_composite(bg, (4 + i * S, 18))
    d.text((6 + i * S, 4), lb, fill=(255, 255, 120, 255))
cv.convert("RGB").save("Assets/Sprites/Mannequin/Diagnostics/_final_check2.png")
print("montage done")
