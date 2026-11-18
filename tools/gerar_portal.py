# Desenha Art/Effects/PortalSwirl.png: um redemoinho branco de 4 braços em espiral, mais
# forte perto da borda e sumindo no centro. É branco de propósito: a cor vem do Sprite
# Renderer (Color multiplica a imagem), então a mesma imagem serve para as duas camadas
# do portal (roxo por fora, rosa por dentro).
# Uso: python3 tools/gerar_portal.py Assets/_Project/Art/Effects/PortalSwirl.png
import zlib, struct, sys, math

N = 256      # 256 x 256 px = 2,56 unidades a 100 px por unidade
ARMS = 4     # número de braços da espiral
TWIST = 0.45 # quanto cada braço enrola do centro até a borda (em voltas)
SS = 2       # supersampling 2 x 2 para a borda não serrilhar

def alpha_at(x, y):
    dx = x - N / 2; dy = y - N / 2
    r = math.hypot(dx, dy) / (N / 2)       # 0 no centro, 1 na borda
    if r >= 1.0:
        return 0.0
    angle = math.atan2(dy, dx)
    # Espiral: o braço está onde (ângulo + torção * raio) dá um múltiplo de 2*pi/ARMS.
    phase = ARMS * (angle + TWIST * 2 * math.pi * r)
    arm = (math.cos(phase) + 1) / 2          # 1 no meio do braço, 0 entre braços
    arm = arm ** 3                            # braços mais finos
    # Anel: forte por volta de 70% do raio; some no centro (o "buraco") e na borda.
    ring = math.exp(-((r - 0.7) / 0.28) ** 2)
    rim = math.exp(-((r - 0.9) / 0.05) ** 2)  # contorno fino e claro na borda
    return min(1.0, arm * ring + 0.6 * rim)

rows = []
for y in range(N):
    row = bytearray([0])  # filtro 0 do PNG em cada linha
    for x in range(N):
        a = sum(alpha_at(x + (sx + 0.5) / SS, y + (sy + 0.5) / SS)
                for sy in range(SS) for sx in range(SS)) / (SS * SS)
        row += bytes([255, 255, 255, round(255 * a)])
    rows.append(bytes(row))

def chunk(tag, data):
    return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', zlib.crc32(tag + data) & 0xffffffff)

png = (b'\x89PNG\r\n\x1a\n'
       + chunk(b'IHDR', struct.pack('>IIBBBBB', N, N, 8, 6, 0, 0, 0))
       + chunk(b'IDAT', zlib.compress(b''.join(rows), 9))
       + chunk(b'IEND', b''))
open(sys.argv[1], 'wb').write(png)
