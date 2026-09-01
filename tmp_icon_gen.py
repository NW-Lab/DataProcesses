import struct
import zlib
from pathlib import Path

root = Path('src/DataProcesses.Nodes.BuiltIn/Blocks')


def encode_png(img):
    height = len(img)
    width = len(img[0]) if img else 0
    raw = bytearray()
    for row in img:
        raw.append(0)
        for r, g, b, a in row:
            raw.extend((r, g, b, a))
    compressed = zlib.compress(bytes(raw))

    def chunk(chunk_type, data):
        return struct.pack('>I', len(data)) + chunk_type + data + struct.pack('>I', zlib.crc32(chunk_type + data) & 0xffffffff)

    png = bytearray(b'\x89PNG\r\n\x1a\n')
    png.extend(chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 6, 8, 2, 2, 0)))
    png.extend(chunk(b'IDAT', compressed))
    png.extend(chunk(b'IEND', b''))
    return bytes(png)


def make_icon(path: Path, kind: str):
    w = h = 64
    img = [[(245, 247, 250, 255) for _ in range(w)] for _ in range(h)]

    def fill_rect(x0, y0, x1, y1, color):
        for y in range(max(0, y0), min(h, y1)):
            for x in range(max(0, x0), min(w, x1)):
                img[y][x] = color

    def draw_line(x0, y0, x1, y1, color, thickness=2):
        dx = abs(x1 - x0)
        dy = -abs(y1 - y0)
        sx = 1 if x0 < x1 else -1
        sy = 1 if y0 < y1 else -1
        err = dx + dy
        while True:
            for yy in range(max(0, y0 - thickness), min(h, y0 + thickness + 1)):
                for xx in range(max(0, x0 - thickness), min(w, x0 + thickness + 1)):
                    img[yy][xx] = color
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy

    fill_rect(0, 0, w, h, (245, 247, 250, 255))
    fill_rect(8, 8, w - 8, h - 8, (255, 255, 255, 255))

    if kind == 'camera':
        navy = (30, 64, 108, 255)
        blue = (54, 135, 212, 255)
        fill_rect(12, 24, 52, 48, navy)
        fill_rect(20, 19, 34, 25, navy)
        fill_rect(16, 28, 48, 44, blue)
        for y in range(29, 44):
            for x in range(17, 48):
                if (x - 32) ** 2 + (y - 36) ** 2 <= 49:
                    img[y][x] = (245, 247, 250, 255)
                if (x - 32) ** 2 + (y - 36) ** 2 <= 25:
                    img[y][x] = navy
    elif kind == 'movie':
        navy = (30, 64, 108, 255)
        blue = (54, 135, 212, 255)
        fill_rect(12, 16, 52, 48, navy)
        fill_rect(17, 20, 47, 44, blue)
        for y in [20, 28, 36]:
            fill_rect(12, y, 16, y + 4, (245, 247, 250, 255))
            fill_rect(48, y, 52, y + 4, (245, 247, 250, 255))
        for y in range(26, 39):
            for x in range(25, 39):
                if x - 25 <= (y - 26) * 0.9 and x - 25 <= (38 - y) * 0.9:
                    img[y][x] = (245, 247, 250, 255)
    elif kind == 'vec':
        fill_rect(12, 12, 52, 52, (240, 244, 250, 255))
        blue = (65, 126, 240, 255)
        for i, x in enumerate([16, 22, 28, 34, 40, 46, 52]):
            hbar = 8 + (i % 3) * 6
            y = 44 - hbar
            fill_rect(x, y, x + 4, 50, blue)
        draw_line(16, 36, 52, 22, (30, 87, 198, 255), thickness=2)
        for x, y in [(20, 34), (28, 28), (36, 31), (44, 25), (50, 22)]:
            fill_rect(x, y, x + 2, y + 2, (30, 87, 198, 255))
    elif kind == 'breath_st':
        import math
        teal = (13, 148, 136, 255)
        points = []
        for x in range(14, 51):
            y = 32 + int(14 * math.sin((x - 14) / 36 * math.pi * 2))
            points.append((x, y))
        for (x0, y0), (x1, y1) in zip(points, points[1:]):
            draw_line(x0, y0, x1, y1, teal, thickness=2)
    elif kind == 'breath_image':
        import math
        navy = (30, 64, 108, 255)
        blue = (54, 135, 212, 255)
        for x0, y0, x1, y1 in [(12, 12, 20, 14), (12, 12, 14, 20), (44, 12, 52, 14), (50, 12, 52, 20),
                               (12, 44, 14, 52), (12, 50, 20, 52), (44, 50, 52, 52), (50, 44, 52, 52)]:
            fill_rect(x0, y0, x1, y1, navy)
        points = []
        for x in range(18, 47):
            y = 32 + int(9 * math.sin((x - 18) / 29 * math.pi * 2))
            points.append((x, y))
        for (x0, y0), (x1, y1) in zip(points, points[1:]):
            draw_line(x0, y0, x1, y1, blue, thickness=2)
    elif kind == 'cd_time':
        import math
        gray = (150, 158, 168, 255)
        orange = (249, 115, 22, 255)
        navy = (30, 64, 108, 255)
        points = []
        for x in range(14, 51):
            y = 34 + int(12 * math.sin((x - 14) / 37 * math.pi * 2))
            points.append((x, y))
        for (x0, y0), (x1, y1) in zip(points, points[1:]):
            draw_line(x0, y0, x1, y1, gray, thickness=1)
        mx, my = points[len(points) // 2]
        slope = (points[len(points) // 2 + 1][1] - points[len(points) // 2 - 1][1]) / 2
        draw_line(mx - 12, int(my - slope * 12), mx + 12, int(my + slope * 12), orange, thickness=2)
        fill_rect(mx - 2, my - 2, mx + 2, my + 2, navy)
    elif kind == 'moving_avg':
        import math, random
        rnd = random.Random(7)
        gray = (170, 176, 184, 255)
        blue = (54, 135, 212, 255)
        noisy = []
        smooth = []
        for x in range(14, 51):
            base = 32 + 10 * math.sin((x - 14) / 37 * math.pi * 2)
            noisy.append((x, int(base + rnd.uniform(-6, 6))))
            smooth.append((x, int(base)))
        for (x0, y0), (x1, y1) in zip(noisy, noisy[1:]):
            draw_line(x0, y0, x1, y1, gray, thickness=1)
        for (x0, y0), (x1, y1) in zip(smooth, smooth[1:]):
            draw_line(x0, y0, x1, y1, blue, thickness=2)
    else:
        fill_rect(14, 14, 50, 50, (240, 240, 240, 255))
        fill_rect(18, 18, 46, 46, (251, 251, 251, 255))
        magenta = (219, 39, 119, 255)
        orange = (249, 115, 22, 255)
        for y in range(20, 42, 8):
            for x in range(21, 43, 8):
                fill_rect(x, y, x + 4, y + 4, magenta)
        for y in range(24, 46, 8):
            for x in range(25, 47, 8):
                fill_rect(x, y, x + 4, y + 4, orange)
        fill_rect(18, 46, 22, 50, (30, 87, 198, 255))
        fill_rect(42, 46, 46, 50, (30, 87, 198, 255))
        fill_rect(18, 50, 46, 54, (30, 87, 198, 255))

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(encode_png(img))

make_icon(root / 'TestSignalVec' / 'icon.png', 'vec')
make_icon(root / 'TestSignalImg' / 'icon.png', 'img')
make_icon(root / 'CameraInputImage' / 'icon.png', 'camera')
make_icon(root / 'MovieInputImage' / 'icon.png', 'movie')
make_icon(root / 'BreathSt' / 'icon.png', 'breath_st')
make_icon(root / 'BreathImage' / 'icon.png', 'breath_image')
make_icon(root / 'CdTimeResolvedMethodSt' / 'icon.png', 'cd_time')
make_icon(root / 'MovingAverage' / 'icon.png', 'moving_avg')
print('created', root / 'TestSignalVec' / 'icon.png')
print('created', root / 'TestSignalImg' / 'icon.png')
print('created', root / 'CameraInputImage' / 'icon.png')
print('created', root / 'MovieInputImage' / 'icon.png')
print('created', root / 'BreathSt' / 'icon.png')
print('created', root / 'BreathImage' / 'icon.png')
print('created', root / 'CdTimeResolvedMethodSt' / 'icon.png')
print('created', root / 'MovingAverage' / 'icon.png')
