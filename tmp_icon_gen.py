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

    if kind == 'vec':
        fill_rect(12, 12, 52, 52, (240, 244, 250, 255))
        blue = (65, 126, 240, 255)
        for i, x in enumerate([16, 22, 28, 34, 40, 46, 52]):
            hbar = 8 + (i % 3) * 6
            y = 44 - hbar
            fill_rect(x, y, x + 4, 50, blue)
        draw_line(16, 36, 52, 22, (30, 87, 198, 255), thickness=2)
        for x, y in [(20, 34), (28, 28), (36, 31), (44, 25), (50, 22)]:
            fill_rect(x, y, x + 2, y + 2, (30, 87, 198, 255))
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
print('created', root / 'TestSignalVec' / 'icon.png')
print('created', root / 'TestSignalImg' / 'icon.png')
