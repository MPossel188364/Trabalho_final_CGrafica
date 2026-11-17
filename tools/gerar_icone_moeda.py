import zlib, struct, sys, math
N=64; SS=4  # 64x64, 4x4 supersampling para borda suave
# "$" em bitmap 9x13 (1 = pixel do símbolo)
DOLLAR = [
"....#....",
"..#####..",
".##.#.##.",
".##.#....",
".##.#....",
"..####...",
"....###..",
"....#.##.",
"....#.##.",
".##.#.##.",
"..#####..",
"....#....",
".........",
]
GOLD=(255,204,64); LIGHT=(255,226,120); RIM=(196,128,20); DARK=(150,90,10)
def sample(x,y):
    cx=cy=N/2; dx=x-cx; dy=y-cy; d=math.hypot(dx,dy)
    if d>30: return None
    if d>26: return DARK                       # contorno externo
    if d>22: return RIM if dy>0 else GOLD      # anel com sombra embaixo
    # símbolo: bitmap centrado, cada célula 3.6 px
    cell=3.4; w=len(DOLLAR[0])*cell; h=len(DOLLAR)*cell
    gx=int((x-(cx-w/2))/cell); gy=int((y-(cy-h/2)+1.5)/cell)
    if 0<=gy<len(DOLLAR) and 0<=gx<len(DOLLAR[0]) and DOLLAR[gy][gx]=='#': return DARK
    return LIGHT if (dx+dy)<-14 else GOLD      # brilho no canto superior esquerdo
rows=[]
for y in range(N):
    row=bytearray([0])
    for x in range(N):
        acc=[0,0,0,0]
        for sy in range(SS):
            for sx in range(SS):
                c=sample(x+(sx+.5)/SS, y+(sy+.5)/SS)
                if c: acc[0]+=c[0];acc[1]+=c[1];acc[2]+=c[2];acc[3]+=1
        n=acc[3]
        row+=bytes([acc[0]//n,acc[1]//n,acc[2]//n,255*n//(SS*SS)] if n else [0,0,0,0])
    rows.append(bytes(row))
def chunk(t,d): return struct.pack('>I',len(d))+t+d+struct.pack('>I',zlib.crc32(t+d)&0xffffffff)
png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',N,N,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(b''.join(rows),9))+chunk(b'IEND',b'')
open(sys.argv[1],'wb').write(png)
