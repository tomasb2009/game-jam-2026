"""Genera la banda sonora y los efectos originales de THE WARRIOR PATH.

Todo se sintetiza localmente (ondas cuadradas/triangulares/seno + ruido): son
pistas originales, sin copyright y sin depender de ningun servicio externo.

Uso:  python generar_audio.py <carpeta_de_salida>
"""
import math
import os
import struct
import sys
import wave

SR = 22050  # muestras por segundo (mono, 16 bits)


# ----------------------------- utilidades de audio -----------------------------

def nota(nombre, octava=4):
    """Frecuencia de una nota con notacion anglosajona (C, Cs, D, ...)."""
    base = {"C": 0, "Cs": 1, "D": 2, "Ds": 3, "E": 4, "F": 5,
            "Fs": 6, "G": 7, "Gs": 8, "A": 9, "As": 10, "B": 11}[nombre]
    midi = 12 * (octava + 1) + base
    return 440.0 * (2 ** ((midi - 69) / 12.0))


class Buffer(object):
    """Pista de audio en memoria, mezcla por suma con saturacion suave."""

    def __init__(self, segundos):
        self.n = int(segundos * SR)
        self.datos = [0.0] * self.n

    def sumar(self, inicio, muestra):
        i = int(inicio * SR)
        for k, v in enumerate(muestra):
            j = i + k
            if 0 <= j < self.n:
                self.datos[j] += v

    def escribir(self, ruta, normalizar=True, suavizar_union=0.0):
        pico = max(1e-9, max(abs(v) for v in self.datos)) if normalizar else 1.0
        escala = (0.89 / pico) if normalizar else 1.0

        if suavizar_union > 0:
            # Microfade en la union del loop: evita el chasquido al repetir
            n_fade = int(suavizar_union * SR)
            for i in range(min(n_fade, self.n)):
                factor = i / n_fade
                self.datos[i] *= factor
                self.datos[self.n - 1 - i] *= factor

        with wave.open(ruta, "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(SR)
            cuadros = bytearray()
            for v in self.datos:
                # saturacion suave para que no suene duro al mezclar
                x = math.tanh(v * escala * 1.15)
                cuadros += struct.pack("<h", int(max(-1.0, min(1.0, x)) * 32000))
            w.writeframes(bytes(cuadros))


def envolvente(n, ataque=0.01, caida=0.25, sostenido=0.6, liberacion=0.2):
    """ADSR simple en cantidad de muestras."""
    env = []
    a = max(1, int(ataque * SR))
    d = max(1, int(caida * SR))
    r = max(1, int(liberacion * SR))
    s = max(1, n - a - d - r)
    for i in range(a):
        env.append(i / a)
    for i in range(d):
        env.append(1.0 - (1.0 - sostenido) * (i / d))
    for _ in range(s):
        env.append(sostenido)
    for i in range(r):
        env.append(sostenido * (1.0 - i / r))
    return env[:n] + [0.0] * max(0, n - len(env))


def osc_triangular(freq, segundos, fase=0.0):
    n = int(segundos * SR)
    salida = []
    for i in range(n):
        t = (i / SR) * freq + fase
        x = t - math.floor(t + 0.5)
        salida.append(2.0 * abs(2.0 * x) - 1.0)
    return salida


def osc_pulso(freq, segundos, ancho=0.5, fase=0.0):
    n = int(segundos * SR)
    salida = []
    fase_total = fase
    for i in range(n):
        fase_total += freq / SR
        salida.append(1.0 if (fase_total - math.floor(fase_total)) < ancho else -1.0)
    return salida


def osc_seno(freq, segundos, fase=0.0):
    n = int(segundos * SR)
    return [math.sin(2 * math.pi * freq * (i / SR) + fase) for i in range(n)]


def ruido(segundos, semilla=1):
    n = int(segundos * SR)
    estado = semilla
    salida = []
    for _ in range(n):
        estado = (1103515245 * estado + 12345) % (2 ** 31)
        salida.append((estado / (2 ** 31 - 1)) * 2.0 - 1.0)
    return salida


def mezclar(*pistas):
    n = max(len(p) for p in pistas)
    salida = [0.0] * n
    for pista in pistas:
        for i, v in enumerate(pista):
            salida[i] += v
    return salida


def escalar(pista, ganancia):
    return [v * ganancia for v in pista]


def armar(tono, segundos, ataque=0.005, sostenido=0.7, liberacion=0.15, ganancia=0.3):
    env = envolvente(len(tono), ataque, 0.08, sostenido, liberacion)
    return [tono[i] * env[i] * ganancia for i in range(min(len(tono), len(env)))]


def pluck(freq, segundos, ancho=0.35, ganancia=0.3):
    return armar(osc_pulso(freq, segundos, ancho), segundos, 0.004, 0.35, 0.3, ganancia)


def bajo(freq, segundos, ganancia=0.35):
    return armar(osc_triangular(freq, segundos), segundos, 0.008, 0.6, 0.2, ganancia)


def pad(freq, segundos, ganancia=0.14):
    return armar(osc_seno(freq, segundos), segundos, 0.4, 0.85, 0.6, ganancia)


# ------------------------------- percusion -------------------------------

def bombo(ganancia=0.5):
    segundos = 0.22
    n = int(segundos * SR)
    salida = []
    for i in range(n):
        t = i / SR
        freq = 110 * math.exp(-t * 22) + 42
        salida.append(math.sin(2 * math.pi * freq * t) * math.exp(-t * 9) * ganancia)
    return salida


def caja(ganancia=0.3, semilla=7):
    segundos = 0.18
    r = ruido(segundos, semilla)
    salida = []
    for i, v in enumerate(r):
        t = i / SR
        salida.append(v * math.exp(-t * 24) * ganancia)
    return salida


def charles(ganancia=0.14, semilla=13):
    segundos = 0.06
    r = ruido(segundos, semilla)
    return [v * math.exp(-(i / SR) * 90) * ganancia for i, v in enumerate(r)]


# ------------------------------- pistas -------------------------------

def pista(bpm, compases, patron_melodia, patron_bajo, acordes, percusion=True,
          timbre="pluck", ganancia_pad=0.12, swing=False):
    """patron_melodia puede ser una frase o una lista de frases (se alternan)."""
    if patron_melodia and isinstance(patron_melodia[0], tuple):
        patron_melodia = [patron_melodia]

    """Compone un loop exacto de 'compases' compases de 4 tiempos."""
    negra = 60.0 / bpm
    total = negra * 4 * compases
    buf = Buffer(total)

    for compas in range(compases):
        inicio_compas = compas * negra * 4

        # bajo + pad sobre el acorde del compas
        raiz = acordes[compas % len(acordes)][0]
        buf.sumar(inicio_compas, bajo(nota(raiz, 2), negra * 4, 0.4))
        for nombre in acordes[compas % len(acordes)]:
            buf.sumar(inicio_compas, pad(nota(nombre, 3), negra * 4, ganancia_pad))

        # melodia: alterna frases cada 4 compases para no sonar repetitivo
        frase = patron_melodia[(compas // 4) % len(patron_melodia)]
        for tiempo, (nombre, octava, dur, silencio) in enumerate(frase):
            if compas == 0 and silencio:
                pass
            if nombre is None:
                continue
            if silencio and compas % 2 == 1:
                continue
            offset = inicio_compas + tiempo * negra
            if swing and tiempo % 2 == 1:
                offset += negra * 0.12
            freq = nota(nombre, octava)
            if timbre == "pluck":
                buf.sumar(offset, pluck(freq, negra * dur, 0.3, 0.26))
            elif timbre == "pulso":
                buf.sumar(offset, armar(osc_pulso(freq, negra * dur, 0.5), negra * dur, 0.006, 0.5, 0.25, 0.22))
            else:
                buf.sumar(offset, armar(osc_triangular(freq, negra * dur), negra * dur, 0.01, 0.5, 0.3, 0.24))

        # percusion
        if percusion:
            for tiempo in range(4):
                offset = inicio_compas + tiempo * negra
                if tiempo in (0, 2):
                    buf.sumar(offset, bombo(0.42))
                if tiempo in (1, 3):
                    buf.sumar(offset, caja(0.22, 7 + compas))
                for sub in (0.5,):
                    buf.sumar(offset + negra * sub, charles(0.1, 13 + tiempo))
            if patron_bajo == "corcheas":
                for tiempo in range(8):
                    freq = nota(raiz, 2)
                    buf.sumar(inicio_compas + tiempo * negra * 0.5, bajo(freq, negra * 0.45, 0.22))

    return buf


def generar_salida(carpeta):
    if not os.path.isdir(carpeta):
        os.makedirs(carpeta)

    ruta = lambda nombre: os.path.join(carpeta, nombre + ".wav")

    # --- Menu: misterioso, lento, menor natural ---
    frase_menu_a = [("A", 4, 2, False), (None, 0, 0, False), ("C", 5, 1, False), ("B", 4, 1, False),
                    ("A", 4, 2, False), ("E", 4, 1, False), (None, 0, 0, False), ("G", 4, 1, False)]
    frase_menu_b = [("E", 4, 1, False), ("F", 4, 1, False), ("G", 4, 2, False),
                    ("B", 4, 1, False), ("C", 5, 1, False), ("B", 4, 1, False), ("A", 4, 1, False)]
    acordes_menu = [("A", "C", "E"), ("F", "A", "C"), ("G", "B", "D"), ("E", "G", "B")]
    pista_menu = pista(76, 8, [frase_menu_a, frase_menu_b], "largas", acordes_menu, percusion=True, timbre="triangular", ganancia_pad=0.13)
    pista_menu.escribir(ruta("musica_menu"), suavizar_union=0.02)

    # --- Pueblo: folk alegre, mayor ---
    frase_pueblo_a = [("D", 5, 1, False), ("F", 5, 1, False), ("A", 5, 2, False),
                      ("G", 5, 1, False), ("F", 5, 1, False), ("D", 5, 1, False), ("E", 5, 1, False)]
    frase_pueblo_b = [("A", 4, 1, False), ("D", 5, 1, False), ("F", 5, 2, False),
                      ("E", 5, 1, False), ("D", 5, 1, False), ("C", 5, 1, False), ("D", 5, 1, False)]
    acordes_pueblo = [("D", "F", "A"), ("G", "B", "D"), ("A", "C", "E"), ("D", "F", "A")]
    pista_pueblo = pista(112, 8, [frase_pueblo_a, frase_pueblo_b], "corcheas", acordes_pueblo, percusion=True, timbre="pluck", ganancia_pad=0.1)
    pista_pueblo.escribir(ruta("musica_pueblo"), suavizar_union=0.02)

    # --- Caverna: oscuro, espaciado ---
    frase_caverna_a = [("D", 4, 3, False), (None, 0, 0, False), ("F", 4, 2, False), (None, 0, 0, False),
                       ("A", 3, 2, False), (None, 0, 0, False), ("C", 4, 1, False), (None, 0, 0, False)]
    frase_caverna_b = [("F", 4, 2, False), (None, 0, 0, False), ("E", 4, 2, False), (None, 0, 0, False),
                       ("D", 4, 2, False), (None, 0, 0, False), ("As", 3, 2, False)]
    acordes_caverna = [("D", "F", "A"), ("D", "F", "A"), ("As", "D", "F"), ("C", "E", "G")]
    pista_caverna = pista(68, 8, [frase_caverna_a, frase_caverna_b], "largas", acordes_caverna, percusion=False, timbre="triangular", ganancia_pad=0.16)
    pista_caverna.escribir(ruta("musica_caverna"), suavizar_union=0.02)

    # --- Jefe: rapido y agresivo ---
    frase_jefe_a = [("E", 4, 1, False), ("E", 4, 1, False), ("G", 4, 1, False), ("A", 4, 1, False),
                    ("B", 4, 1, False), ("A", 4, 1, False), ("G", 4, 1, False), ("E", 4, 1, False)]
    frase_jefe_b = [("C", 5, 1, False), ("B", 4, 1, False), ("A", 4, 1, False), ("G", 4, 1, False),
                    ("A", 4, 1, False), ("B", 4, 1, False), ("C", 5, 2, False)]
    acordes_jefe = [("E", "G", "B"), ("C", "E", "G"), ("D", "Fs", "A"), ("E", "G", "B")]
    pista_jefe = pista(148, 8, [frase_jefe_a, frase_jefe_b], "corcheas", acordes_jefe, percusion=True, timbre="pulso", ganancia_pad=0.08)
    pista_jefe.escribir(ruta("musica_jefe"), suavizar_union=0.02)

    # --- Epilogo: calmado y esperanzador ---
    frase_epilogo_a = [("C", 5, 2, False), ("E", 5, 1, False), ("G", 5, 1, False),
                       ("F", 5, 2, False), ("E", 5, 2, False)]
    frase_epilogo_b = [("A", 4, 2, False), ("C", 5, 1, False), ("D", 5, 1, False),
                       ("E", 5, 2, False), ("C", 5, 2, False)]
    acordes_epilogo = [("C", "E", "G"), ("F", "A", "C"), ("G", "B", "D"), ("C", "E", "G")]
    pista_epilogo = pista(72, 8, [frase_epilogo_a, frase_epilogo_b], "largas", acordes_epilogo, percusion=False, timbre="triangular", ganancia_pad=0.15)
    pista_epilogo.escribir(ruta("musica_epilogo"), suavizar_union=0.02)

    # ------------------------------ efectos ------------------------------

    # Carga del arco: crujido ascendente
    buf = Buffer(0.55)
    n = int(0.5 * SR)
    tono = []
    fase_subida = 0.0
    for i in range(n):
        t = i / SR
        freq = 180 + 420 * (t / 0.5)
        fase_subida += freq / SR
        x = fase_subida - math.floor(fase_subida + 0.5)
        triangular = 2.0 * abs(2.0 * x) - 1.0
        tono.append(triangular * (0.25 + 0.75 * (t / 0.5)))
    buf.sumar(0.02, armar(tono, 0.5, 0.05, 0.8, 0.1, 0.22))
    buf.sumar(0.02, escalar(ruido(0.5, 3), 0.05))
    buf.escribir(ruta("sfx_arco_carga"))

    # Disparo del arco: silbido corto
    buf = Buffer(0.3)
    buf.sumar(0.0, escalar(ruido(0.22, 5), 0.28))
    buf.sumar(0.0, armar(osc_seno(900, 0.22), 0.22, 0.002, 0.2, 0.2, 0.18))
    buf.escribir(ruta("sfx_arco_disparo"))

    # Victoria: arpegio mayor ascendente
    buf = Buffer(2.2)
    for i, (nombre, octava) in enumerate([("C", 5), ("E", 5), ("G", 5), ("C", 6)]):
        buf.sumar(0.12 * i, pluck(nota(nombre, octava), 1.1, 0.5, 0.3))
    for nombre in ("C", "E", "G"):
        buf.sumar(0.55, pad(nota(nombre, 4), 1.6, 0.16))
    buf.escribir(ruta("sfx_victoria"))

    # Derrota: descenso menor
    buf = Buffer(2.2)
    for i, (nombre, octava) in enumerate([("A", 4), ("G", 4), ("F", 4), ("E", 4)]):
        buf.sumar(0.16 * i, pluck(nota(nombre, octava), 1.0, 0.25, 0.3))
    for nombre in ("A", "C", "E"):
        buf.sumar(0.7, pad(nota(nombre, 3), 1.5, 0.18))
    buf.escribir(ruta("sfx_derrota"))

    # Golpe del jefe: impacto grave
    buf = Buffer(0.5)
    buf.sumar(0.0, bombo(0.55))
    buf.sumar(0.0, escalar(ruido(0.3, 11), 0.25))
    buf.sumar(0.02, armar(osc_triangular(70, 0.35), 0.35, 0.002, 0.3, 0.4, 0.3))
    buf.escribir(ruta("sfx_golpe_jefe"))

    print("listo: pistas y efectos escritos en", carpeta)


if __name__ == "__main__":
    destino = sys.argv[1] if len(sys.argv) > 1 else "."
    generar_salida(destino)
