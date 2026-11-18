# Desenha Art/Effects/Particle.png: um disco branco de borda suave, a "bolinha" das
# partículas de explosão. É branco de propósito: a cor vem do Particle System
# (Start Color e Color over Lifetime), que multiplica a imagem.
# Uso: python3 tools/gerar_particula.py Assets/_Project/Art/Effects/Particle.png
import zlib, struct, sys, math

N = 64  # 64 x 64 px; a 100 px por unidade, 0,64 unidade com Start Size 1

rows = []
for y in range(N):
    row = bytearray([0])  # filtro 0 do PNG em cada linha
    for x in range(N):
        # distância ao centro, de 0 (meio) a 1 (borda do círculo)
        d = math.hypot(x + 0.5 - N / 2, y + 0.5 - N / 2) / (N / 2)
        # miolo cheio até 35% do raio, depois cai suave (smoothstep) até 0 na borda
        t = min(max((d - 0.35) / 0.65, 0.0), 1.0)
        alpha = 1.0 - t * t * (3 - 2 * t)
        row += bytes([255, 255, 255, round(255 * alpha)])
    rows.append(bytes(row))

def chunk(tag, data):
    return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', zlib.crc32(tag + data) & 0xffffffff)

png = (b'\x89PNG\r\n\x1a\n'
       + chunk(b'IHDR', struct.pack('>IIBBBBB', N, N, 8, 6, 0, 0, 0))
       + chunk(b'IDAT', zlib.compress(b''.join(rows), 9))
       + chunk(b'IEND', b''))
open(sys.argv[1], 'wb').write(png)
