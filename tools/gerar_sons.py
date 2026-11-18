#!/usr/bin/env python3
"""
Gera os sons do jogo por síntese (sem baixar nada, sem direitos autorais).

Uso (na raiz do repositório):
    python3 tools/gerar_sons.py

Precisa de: numpy, scipy e ffmpeg (só para a música em .ogg).
Escreve em Assets/_Project/Audio/Resources/{Sfx,Music}/ e cria os .meta com GUID fixo
(derivado do caminho), então rodar de novo não troca nada no Git se o som não mudou.

Os efeitos são .wav curtos (mono, 44,1 kHz); a música são dois loops em .ogg.
Para mudar um som: edite a função dele aqui e rode o script de novo.
"""
import hashlib
import subprocess
import wave
from pathlib import Path

import numpy as np
from scipy import signal

SR = 44100
RAIZ = Path(__file__).resolve().parent.parent
AUDIO = RAIZ / "Assets" / "_Project" / "Audio"
rng = np.random.default_rng(300)  # semente fixa: o mesmo som toda vez


# ---------------------------------------------------------------- blocos básicos
def t_(dur):
    return np.arange(int(SR * dur)) / SR


def env_exp(n, decay):
    """Ataque de 2 ms e queda exponencial. 'decay' maior = some mais rápido."""
    t = np.arange(n) / SR
    e = np.exp(-decay * t)
    a = max(1, int(0.002 * SR))
    e[:a] *= np.linspace(0, 1, a)
    return e


def fade_out(x, ms=8):
    n = min(len(x), int(SR * ms / 1000))
    x[-n:] *= np.linspace(1, 0, n)
    return x


def sweep(f0, f1, dur, expo=True):
    """Seno cuja frequência vai de f0 a f1."""
    t = t_(dur)
    if expo:
        k = np.log(f1 / f0) / dur
        phase = 2 * np.pi * f0 * (np.exp(k * t) - 1) / k
    else:
        phase = 2 * np.pi * (f0 * t + (f1 - f0) * t ** 2 / (2 * dur))
    return np.sin(phase)


def noise(dur):
    return rng.uniform(-1, 1, int(SR * dur))


def lowpass(x, fc, order=4):
    return signal.sosfilt(signal.butter(order, fc, "low", fs=SR, output="sos"), x)


def highpass(x, fc, order=4):
    return signal.sosfilt(signal.butter(order, fc, "high", fs=SR, output="sos"), x)


def bandpass(x, lo, hi, order=3):
    return signal.sosfilt(signal.butter(order, [lo, hi], "band", fs=SR, output="sos"), x)


def sweep_noise(dur, f0, f1):
    """Ruído cujo corte do passa-baixa desliza de f0 a f1 (rugido de explosão, whoosh)."""
    x = noise(dur)
    n = len(x)
    out = np.zeros(n)
    blocos = 24
    tam = n // blocos + 1
    zi = None
    for i in range(blocos):
        fc = f0 * (f1 / f0) ** (i / (blocos - 1))
        sos = signal.butter(2, min(fc, SR / 2 - 100), "low", fs=SR, output="sos")
        if zi is None:
            zi = np.zeros((sos.shape[0], 2))
        trecho = x[i * tam:(i + 1) * tam]
        if len(trecho) == 0:
            break
        y, zi = signal.sosfilt(sos, trecho, zi=zi)
        out[i * tam:i * tam + len(y)] = y
    return out


def norm(x, pico=0.9):
    m = np.max(np.abs(x))
    return x / m * pico if m > 0 else x


def nota(freq, dur, decay=6.0, harm=(1.0, 0.3, 0.1)):
    t = t_(dur)
    s = sum(a * np.sin(2 * np.pi * freq * (i + 1) * t) for i, a in enumerate(harm))
    return s * env_exp(len(t), decay)


def mistura(partes, dur):
    """Soma (inicio_s, som) numa faixa de 'dur' segundos."""
    out = np.zeros(int(SR * dur))
    for inicio, s in partes:
        i = int(SR * inicio)
        fim = min(len(out), i + len(s))
        if fim > i:
            out[i:fim] += s[:fim - i]
    return out


# ---------------------------------------------------------------- efeitos
def laser():  # tiro da Caça: "pew" curto e agudo
    d = 0.16
    s = sweep(2200, 500, d) * env_exp(int(SR * d), 22)
    s += 0.25 * sweep(4400, 1000, d) * env_exp(int(SR * d), 30)
    return fade_out(norm(s, 0.7))


def missile():  # tiro do Bombardeiro: lançamento chiado + baque grave
    d = 0.5
    w = sweep_noise(d, 400, 3500) * np.hanning(int(SR * d)) ** 0.5
    thump = sweep(140, 50, d) * env_exp(int(SR * d), 14)
    return fade_out(norm(0.7 * norm(w) + 0.8 * thump, 0.75))


def railgun():  # tiro do Cruzador: estalo + varredura longa
    d = 0.55
    crack = highpass(noise(d), 2500) * env_exp(int(SR * d), 90)
    zap = sweep(3200, 160, d) * env_exp(int(SR * d), 7)
    boom = sweep(90, 45, d) * env_exp(int(SR * d), 10)
    return fade_out(norm(0.6 * crack + 0.7 * zap + 0.6 * boom, 0.8))


def impact():  # tiro que acerta o inimigo: estalinho
    d = 0.09
    s = bandpass(noise(d), 800, 5000) * env_exp(int(SR * d), 55)
    s += 0.5 * sweep(900, 300, d) * env_exp(int(SR * d), 50)
    return fade_out(norm(s, 0.6), 5)


def explosion():  # inimigo destruído
    d = 0.8
    rugido = sweep_noise(d, 3000, 120) * env_exp(int(SR * d), 5)
    corpo = sweep(110, 35, d) * env_exp(int(SR * d), 6)
    return fade_out(norm(0.8 * norm(rugido) + 0.7 * corpo, 0.85), 20)


def big_explosion():  # explosão do míssil (com área): mais longa e grave
    d = 1.1
    rugido = sweep_noise(d, 2200, 90) * env_exp(int(SR * d), 3.6)
    corpo = sweep(90, 28, d) * env_exp(int(SR * d), 4)
    estalo = highpass(noise(d), 1500) * env_exp(int(SR * d), 40) * 0.5
    return fade_out(norm(0.8 * norm(rugido) + 0.8 * corpo + estalo, 0.9), 25)


def base_hit():  # inimigo bate na nave-mãe: pancada metálica + alarme curto
    d = 1.0
    pancada = sweep(75, 32, d) * env_exp(int(SR * d), 6)
    ruido = sweep_noise(d, 2500, 150) * env_exp(int(SR * d), 7)
    t = t_(d)
    metal = (np.sin(2 * np.pi * 233 * t) + 0.7 * np.sin(2 * np.pi * 349 * t)
             + 0.5 * np.sin(2 * np.pi * 523 * t)) * env_exp(len(t), 5)
    return fade_out(norm(0.9 * pancada + 0.5 * norm(ruido) + 0.25 * metal, 0.9), 25)


def build():  # compra/posicionar nave: três notas subindo + brilho
    partes = [(0.0, nota(523, 0.25, 9)), (0.07, nota(659, 0.25, 9)), (0.14, nota(988, 0.35, 7))]
    s = mistura(partes, 0.5)
    s += 0.3 * highpass(noise(0.5), 5000) * env_exp(len(s), 25)
    return fade_out(norm(s, 0.7))


def sell():  # venda: moedinha (dois tons agudos)
    partes = [(0.0, nota(1319, 0.2, 14, (1, 0.2))), (0.08, nota(1760, 0.35, 9, (1, 0.2)))]
    return fade_out(norm(mistura(partes, 0.45), 0.6))


def spawn():  # inimigo sai do portal: onda grave tremulante
    d = 0.7
    t = t_(d)
    base = sweep(110, 330, d, expo=False)
    trem = 0.6 + 0.4 * np.sin(2 * np.pi * 14 * t)
    forma = np.sin(np.pi * t / d) ** 1.5
    s = (base + 0.4 * sweep(220, 660, d, expo=False)) * trem * forma
    return fade_out(norm(s, 0.45))


def click():  # botões de interface
    d = 0.05
    s = bandpass(noise(d), 1500, 6000) * env_exp(int(SR * d), 90)
    s += 0.6 * sweep(1200, 700, d) * env_exp(int(SR * d), 70)
    return fade_out(norm(s, 0.5), 4)


def victory():  # acorde subindo, tom alegre
    freqs = [523, 659, 784, 1047, 1319]
    partes = [(i * 0.13, nota(f, 1.1 - i * 0.05, 3.2, (1, 0.35, 0.15))) for i, f in enumerate(freqs)]
    return fade_out(norm(mistura(partes, 1.8), 0.8), 40)


def defeat():  # quatro notas descendo, tom triste
    freqs = [392, 349, 311, 196]
    partes = [(i * 0.35, nota(f, 1.2, 3.0, (1, 0.5, 0.2))) for i, f in enumerate(freqs)]
    s = mistura(partes, 2.4)
    s += 0.4 * sweep_noise(2.4, 600, 80) * env_exp(len(s), 2.5) * 0.3
    return fade_out(norm(s, 0.8), 60)


def alarm():  # loop de 1 s: dois bips alternados (base com pouca vida)
    d = 1.0
    n = int(SR * d)
    out = np.zeros(n)
    for ini, f in ((0.0, 880), (0.5, 660)):
        s = np.sign(np.sin(2 * np.pi * f * t_(0.22))) * 0.35 + np.sin(2 * np.pi * f * t_(0.22))
        s = s * np.sin(np.pi * np.linspace(0, 1, len(s))) ** 0.6  # começa e termina em zero
        i = int(SR * ini)
        out[i:i + len(s)] += s
    return norm(out, 0.5)  # termina em silêncio: o fim encaixa no começo


EFEITOS = {
    "laser": laser, "missile": missile, "railgun": railgun, "impact": impact,
    "explosion": explosion, "big_explosion": big_explosion, "base_hit": base_hit,
    "build": build, "sell": sell, "spawn": spawn, "click": click,
    "victory": victory, "defeat": defeat, "alarm": alarm,
}


# ---------------------------------------------------------------- música
def loop_musica(bpm, compassos, acordes, baixo, arp_oitava, pulso, pad_vol, arp_vol):
    """
    Loop de 'compassos' compassos de 4/4. 'acordes' = uma lista de notas (Hz) por compasso.
    O som que sobra depois do fim do loop é somado ao começo, para a emenda ficar sem estalo.
    """
    beat = 60.0 / bpm
    dur = compassos * 4 * beat
    cauda = 2.0
    out = np.zeros(int(SR * (dur + cauda)))
    for c in range(compassos):
        ini = c * 4 * beat
        notas = acordes[c % len(acordes)]
        # pad: acorde longo e suave
        for f in notas:
            t = t_(4 * beat + 0.5)
            pad = (np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * f * 1.004 * t)) * 0.5
            ataque = np.minimum(1, t / 0.6)
            queda = np.minimum(1, (t[-1] - t) / 0.5)
            pad = pad * ataque * queda * pad_vol
            i = int(SR * ini)
            out[i:i + len(pad)] += pad[:len(out) - i]
        # baixo: nota da fundamental, batida por tempo
        fb = baixo[c % len(baixo)]
        for b in range(4 if pulso else 1):
            n_ = nota(fb, beat * (0.9 if pulso else 3.8), 4.0 if pulso else 1.0, (1, 0.4))
            i = int(SR * (ini + b * beat))
            out[i:i + len(n_)] += 0.5 * n_[:len(out) - i]
        # arpejo em colcheias nas notas do acorde, uma oitava acima
        for k in range(8):
            f = notas[k % len(notas)] * arp_oitava
            n_ = nota(f, beat * 0.9, 7.0, (1, 0.25))
            i = int(SR * (ini + k * beat / 2))
            out[i:i + len(n_)] += arp_vol * n_[:len(out) - i]
        # chimbal: ruído agudo curtinho nos contratempos (só na versão com pulso)
        if pulso:
            for k in range(4):
                h = highpass(noise(0.05), 7000) * env_exp(int(SR * 0.05), 90)
                i = int(SR * (ini + (k + 0.5) * beat))
                out[i:i + len(h)] += 0.18 * h[:len(out) - i]
    n = int(SR * dur)
    final = out[:n].copy()
    sobra = out[n:]
    final[:len(sobra)] += sobra
    return norm(final, 0.6)


def musica_menu():  # calma e espacial: Am - F - C - G, 75 bpm, 8 compassos (~25,6 s)
    A, F, C, G = (220, 261.6, 329.6), (174.6, 220, 261.6), (261.6, 329.6, 392), (196, 246.9, 293.7)
    return loop_musica(75, 8, [A, F, C, G], [110, 87.3, 130.8, 98], 2, False, 0.35, 0.22)


def musica_jogo():  # mais tensa e com pulso: Am - G - F - E, 110 bpm, 8 compassos (~17,5 s)
    A, G, F, E = (220, 261.6, 329.6), (196, 246.9, 293.7), (174.6, 220, 261.6), (164.8, 207.7, 246.9)
    return loop_musica(110, 8, [A, G, F, E], [55, 49, 43.65, 41.2], 2, True, 0.25, 0.2)


MUSICAS = {"menu": musica_menu, "jogo": musica_jogo}


# ---------------------------------------------------------------- arquivos
def guid(caminho_relativo):
    return hashlib.md5(("SpaceDefense/" + caminho_relativo).encode()).hexdigest()


META_PASTA = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

META_AUDIO = """fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: {load}
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: {fmt}
    quality: 1
    conversionMode: 0
    preloadAudioData: {preload}
  platformSettingsOverrides: {{}}
  forceToMono: 0
  normalize: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def escrever_meta(arquivo, texto):
    Path(str(arquivo) + ".meta").write_text(texto, encoding="utf-8", newline="\n")


def pasta(p):
    p.mkdir(parents=True, exist_ok=True)
    rel = p.relative_to(RAIZ).as_posix()
    escrever_meta(p, META_PASTA.format(guid=guid(rel)))


def salvar_wav(caminho, x):
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(str(caminho), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


def salvar_ogg(caminho, x):
    tmp = caminho.with_suffix(".tmp.wav")
    salvar_wav(tmp, x)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(tmp), "-c:a", "libvorbis",
                    "-q:a", "4", "-map_metadata", "-1", str(caminho)], check=True)
    tmp.unlink()


def main():
    pasta(AUDIO)
    pasta(AUDIO / "Resources")
    sfx, mus = AUDIO / "Resources" / "Sfx", AUDIO / "Resources" / "Music"
    pasta(sfx)
    pasta(mus)

    for nome, fn in EFEITOS.items():
        arq = sfx / f"{nome}.wav"
        salvar_wav(arq, fn())
        # loadType 0 = Decompress On Load (efeito curto: toca sem atraso); compressionFormat 1 = ADPCM
        escrever_meta(arq, META_AUDIO.format(guid=guid(arq.relative_to(RAIZ).as_posix()),
                                             load=0, fmt=1, preload=1))
    for nome, fn in MUSICAS.items():
        arq = mus / f"{nome}.ogg"
        salvar_ogg(arq, fn())
        # loadType 2 = Streaming (música longa: não ocupa memória); compressionFormat 1 = Vorbis
        escrever_meta(arq, META_AUDIO.format(guid=guid(arq.relative_to(RAIZ).as_posix()),
                                             load=2, fmt=1, preload=0))
    print(f"{len(EFEITOS)} efeitos e {len(MUSICAS)} músicas em {AUDIO.relative_to(RAIZ)}")


if __name__ == "__main__":
    main()
