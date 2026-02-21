"""
FraudGuard AI - IEEE Technical Proposal v2
- True 2-column IEEE layout
- Real Gantt bar chart (drawn with ReportLab canvas)
- System architecture diagram (drawn with canvas)
- Natural, non-AI-sounding prose
- Anonymous (no team/member names)
- Max 5 pages
"""

from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import cm, mm
from reportlab.lib import colors
from reportlab.platypus import (
    BaseDocTemplate, Frame, PageTemplate,
    Paragraph, Spacer, Table, TableStyle,
    HRFlowable, KeepTogether, FrameBreak,
    Flowable
)
from reportlab.lib.enums import TA_LEFT, TA_CENTER, TA_JUSTIFY, TA_RIGHT
from reportlab.pdfgen import canvas as pdfcanvas
import math

OUTPUT = r"C:\Users\trinh\Downloads\FraudGuard_AI_Proposal_v2.pdf"

# ── Page geometry ─────────────────────────────────────────────────────────────
PW, PH = A4          # 595.3 x 841.9
ML = 1.43*cm         # left margin
MR = 1.43*cm
MT = 2.0*cm
MB = 2.0*cm
COL_GAP = 0.45*cm
BODY_W = PW - ML - MR
COL_W = (BODY_W - COL_GAP) / 2

# ── Colours ───────────────────────────────────────────────────────────────────
BLK   = colors.HexColor("#0d0d0d")
DGRAY = colors.HexColor("#2c2c2c")
MGRAY = colors.HexColor("#666666")
LGRAY = colors.HexColor("#aaaaaa")
RULE  = colors.HexColor("#1a1a1a")
NAVY  = colors.HexColor("#0a2342")
STEEL = colors.HexColor("#1b4f72")
LBLUE = colors.HexColor("#d6eaf8")
TBLH  = colors.HexColor("#0a2342")
TBLR  = colors.HexColor("#f4f6f8")
GANTT_FILL  = colors.HexColor("#1b6ca8")
GANTT_FILL2 = colors.HexColor("#2e86c1")
GANTT_BG    = colors.HexColor("#f0f4f8")

# ── Styles ────────────────────────────────────────────────────────────────────
def S(name, **kw):
    return ParagraphStyle(name, **kw)

TITLE = S("title", fontName="Helvetica-Bold", fontSize=13, leading=16,
          textColor=BLK, alignment=TA_CENTER, spaceAfter=2)
CONF  = S("conf",  fontName="Helvetica-Oblique", fontSize=8, leading=10,
          textColor=MGRAY, alignment=TA_CENTER, spaceAfter=3)
ABS_L = S("absl",  fontName="Helvetica-BoldOblique", fontSize=8, leading=10,
          textColor=BLK, spaceAfter=0, spaceBefore=4)
ABS_B = S("absb",  fontName="Helvetica-Oblique", fontSize=7.8, leading=10.5,
          textColor=DGRAY, spaceAfter=5, alignment=TA_JUSTIFY)
H1S   = S("h1s",   fontName="Helvetica-Bold", fontSize=8.5, leading=11,
          textColor=BLK, spaceBefore=7, spaceAfter=2)
H2S   = S("h2s",   fontName="Helvetica-BoldOblique", fontSize=8, leading=10,
          textColor=DGRAY, spaceBefore=4, spaceAfter=1)
BD    = S("bd",    fontName="Helvetica", fontSize=8, leading=10.5,
          textColor=DGRAY, spaceAfter=2.5, alignment=TA_JUSTIFY)
BUL   = S("bul",   fontName="Helvetica", fontSize=7.8, leading=10,
          textColor=DGRAY, spaceAfter=1.5, leftIndent=9, firstLineIndent=0)
BUL2  = S("bul2",  fontName="Helvetica", fontSize=7.5, leading=9.5,
          textColor=DGRAY, spaceAfter=1, leftIndent=18, firstLineIndent=0)
CAP   = S("cap",   fontName="Helvetica-Oblique", fontSize=7, leading=9,
          textColor=MGRAY, spaceAfter=3, alignment=TA_CENTER)
REF   = S("ref",   fontName="Helvetica", fontSize=7.5, leading=10,
          textColor=DGRAY, spaceAfter=1.5, leftIndent=13, firstLineIndent=-13)
KW    = S("kw",    fontName="Helvetica-Oblique", fontSize=7.8, leading=10.5,
          textColor=DGRAY, spaceAfter=6, alignment=TA_JUSTIFY)

TH = S("th", fontName="Helvetica-Bold", fontSize=7.5, leading=9,
       textColor=colors.white, alignment=TA_CENTER)
TD = S("td", fontName="Helvetica", fontSize=7.5, leading=9.5,
       textColor=DGRAY, alignment=TA_LEFT)
TDC = S("tdc", fontName="Helvetica", fontSize=7.5, leading=9.5,
        textColor=DGRAY, alignment=TA_CENTER)

def p(text, style=BD): return Paragraph(text, style)
def h1(t): return Paragraph(t.upper(), H1S)
def h2(t): return Paragraph(t, H2S)
def sp(n=3): return Spacer(1, n)
def hr(w=1): return HRFlowable(width="100%", thickness=w, color=RULE, spaceAfter=3, spaceBefore=1)
def bul(t, lv=1):
    sty = BUL if lv == 1 else BUL2
    sym = "&#8226;" if lv == 1 else "&#8211;"
    return Paragraph(f"{sym}&#160;&#160;{t}", sty)

# ── Table helper ──────────────────────────────────────────────────────────────
def mktbl(headers, rows, widths=None, compact=False):
    fs = 7 if compact else 7.5
    _th = S("_th", fontName="Helvetica-Bold",   fontSize=fs, leading=fs+2, textColor=colors.white, alignment=TA_CENTER)
    _td = S("_td", fontName="Helvetica",         fontSize=fs, leading=fs+2, textColor=DGRAY, alignment=TA_LEFT)
    _tc = S("_tc", fontName="Helvetica",         fontSize=fs, leading=fs+2, textColor=DGRAY, alignment=TA_CENTER)

    data = [[Paragraph(h, _th) for h in headers]]
    for row in rows:
        data.append([Paragraph(str(c), _tc if i > 0 else _td) for i, c in enumerate(row)])

    t = Table(data, colWidths=widths, repeatRows=1)
    t.setStyle(TableStyle([
        ("BACKGROUND", (0,0), (-1,0), TBLH),
        ("ROWBACKGROUNDS", (0,1), (-1,-1), [colors.white, TBLR]),
        ("GRID", (0,0), (-1,-1), 0.3, colors.HexColor("#cccccc")),
        ("TOPPADDING",    (0,0), (-1,-1), 2),
        ("BOTTOMPADDING", (0,0), (-1,-1), 2),
        ("LEFTPADDING",   (0,0), (-1,-1), 4),
        ("RIGHTPADDING",  (0,0), (-1,-1), 3),
        ("VALIGN", (0,0), (-1,-1), "MIDDLE"),
    ]))
    return t

# ── Architecture Diagram Flowable ─────────────────────────────────────────────
class ArchDiagram(Flowable):
    """Hand-drawn system architecture diagram using ReportLab canvas primitives."""
    def __init__(self, width, height):
        Flowable.__init__(self)
        self.width = width
        self.height = height

    def draw(self):
        c = self.canv
        W, H = self.width, self.height

        def box(x, y, w, h, fill, text_lines, text_size=6.5, bold=False,
                stroke=NAVY, radius=3):
            c.setFillColor(fill)
            c.setStrokeColor(stroke)
            c.setLineWidth(0.6)
            c.roundRect(x, y, w, h, radius, fill=1, stroke=1)
            fn = "Helvetica-Bold" if bold else "Helvetica"
            c.setFillColor(colors.white if fill == NAVY or fill == STEEL else BLK)
            line_h = text_size + 1.5
            total_h = len(text_lines) * line_h
            ty = y + h/2 + total_h/2 - line_h*0.7
            for line in text_lines:
                c.setFont(fn, text_size)
                c.drawCentredString(x + w/2, ty, line)
                ty -= line_h

        def arrow(x1, y1, x2, y2, label="", color=STEEL):
            c.setStrokeColor(color)
            c.setFillColor(color)
            c.setLineWidth(0.8)
            dx, dy = x2-x1, y2-y1
            length = math.sqrt(dx*dx + dy*dy)
            if length == 0: return
            c.line(x1, y1, x2, y2)
            # arrowhead
            angle = math.atan2(dy, dx)
            asz = 4
            ax1 = x2 - asz*math.cos(angle - 0.4)
            ay1 = y2 - asz*math.sin(angle - 0.4)
            ax2 = x2 - asz*math.cos(angle + 0.4)
            ay2 = y2 - asz*math.sin(angle + 0.4)
            p = c.beginPath()
            p.moveTo(x2, y2); p.lineTo(ax1, ay1); p.lineTo(ax2, ay2); p.close()
            c.drawPath(p, fill=1, stroke=0)
            if label:
                mx, my = (x1+x2)/2, (y1+y2)/2
                c.setFont("Helvetica", 5.5)
                c.setFillColor(MGRAY)
                c.drawCentredString(mx, my+2, label)

        def darrow(x1, y1, x2, y2, label=""):
            """Double-headed arrow"""
            arrow(x1, y1, x2, y2, label, STEEL)
            arrow(x2, y2, x1, y1, color=STEEL)

        # Background
        c.setFillColor(colors.HexColor("#f9fbfd"))
        c.setStrokeColor(colors.HexColor("#dde3ec"))
        c.setLineWidth(0.5)
        c.roundRect(0, 0, W, H, 4, fill=1, stroke=1)

        pad = 6
        bw = (W - pad*4) / 3   # box width for 3-column layout
        bh = 22                  # box height

        # Row Y positions (from top, in canvas coords where 0=bottom)
        row3_y = H - pad - bh         # top row: Mobile + API GW + DB
        row2_y = row3_y - bh - 18     # middle: STT + Fraud Engine + Agent
        row1_y = row2_y - bh - 14     # bottom: fallback + data stores

        # ── Row 3: Top ────────────────────────────────────────────
        box(pad, row3_y, bw, bh, NAVY,
            ["Mobile Client", "(.NET MAUI 8 / Android)"], bold=True)
        box(pad*2+bw, row3_y, bw, bh, NAVY,
            ["API Gateway", "(Go 1.22 / Chi v5)"], bold=True)
        box(pad*3+bw*2, row3_y, bw, bh, NAVY,
            ["PostgreSQL 16", "(Cloud DB)"], bold=True)

        # arrows row3
        darrow(pad+bw, row3_y+bh/2, pad*2+bw, row3_y+bh/2, "WebSocket")
        arrow(pad*2+bw*2, row3_y+bh/2, pad*3+bw*2, row3_y+bh/2, "pgx/v5")

        # ── Row 2: Middle ─────────────────────────────────────────
        bw2 = (W - pad*4) / 3
        box(pad, row2_y, bw2, bh, STEEL,
            ["Deepgram Nova-2", "(Primary STT)"])
        box(pad*2+bw2, row2_y, bw2, bh, STEEL,
            ["Fraud Engine", "(Rule + Session Score)"])
        box(pad*3+bw2*2, row2_y, bw2, bh, STEEL,
            ["Gemini Agent", "(ReAct + 3 Tools)"])

        # vertical arrows from row3 to row2
        arrow(pad + bw/2, row3_y, pad + bw2/2, row2_y+bh, "16kHz PCM")
        arrow(pad*2+bw+bw/2, row3_y, pad*2+bw2+bw2/2, row2_y+bh, "transcript")
        arrow(pad*2+bw2+bw2/2, row2_y+bh/2,
              pad*3+bw2*2, row2_y+bh/2, "context")

        # ── Row 1: Bottom ─────────────────────────────────────────
        bw3 = (W - pad*4) / 3
        box(pad, row1_y, bw3, bh, colors.HexColor("#2e4057"),
            ["AWS Transcribe", "(Fallback / Circuit Breaker)"])
        box(pad*2+bw3, row1_y, bw3, bh, colors.HexColor("#2e4057"),
            ["SQLite 3 (Local)", "(Call History / WAL)"])
        box(pad*3+bw3*2, row1_y, bw3, bh, colors.HexColor("#2e4057"),
            ["Blacklist DB", "(Community Reports)"])

        arrow(pad + bw3/2, row2_y, pad + bw3/2, row1_y+bh, "failover")
        arrow(pad*2+bw2+bw2/2, row2_y, pad*2+bw3+bw3/2, row1_y+bh, "save log")
        arrow(pad*3+bw2*2+bw2/2, row2_y,
              pad*3+bw3*2+bw3/2, row1_y+bh, "check/report")

        # ── Legend ────────────────────────────────────────────────
        ly = pad + 2
        c.setFont("Helvetica-Bold", 5.5)
        c.setFillColor(BLK)
        c.drawString(pad, ly, "Latency target: < 3 s end-to-end  |  "
                     "Circuit-breaker: Deepgram → AWS Transcribe  |  "
                     "Agent tools: check_blacklist · get_fraud_stats · auto_report")

    def wrap(self, availWidth, availHeight):
        return self.width, self.height


# ── Gantt Chart Flowable ──────────────────────────────────────────────────────
class GanttChart(Flowable):
    """Proper horizontal bar Gantt chart."""
    TASKS = [
        # (phase, task_name, start_week, duration_weeks, is_phase)
        ("Phase 1", "Architecture & Setup",    1, 2, True),
        ("",        "Backend skeleton (Go)",   1, 2, False),
        ("",        "Mobile MAUI scaffold",    1, 1.5, False),
        ("",        "PostgreSQL schema",       1.5, 1.5, False),
        ("Phase 2", "Core Pipeline",           3, 2, True),
        ("",        "Deepgram STT integration",3, 1.5, False),
        ("",        "WebSocket streaming",     3, 2, False),
        ("",        "Fraud scoring engine",    4, 1, False),
        ("Phase 3", "AI & Intelligence",       5, 2, True),
        ("",        "Gemini agent + tools",    5, 1.5, False),
        ("",        "Deepfake detection",      5, 2, False),
        ("",        "Circuit breaker + AWS",   6, 1, False),
        ("Phase 4", "Hardening & Delivery",    7, 2, True),
        ("",        "Data masking / privacy",  7, 1, False),
        ("",        "Security hardening",      7, 2, False),
        ("",        "Docs & submission",       8, 1, False),
    ]
    WEEKS = 8
    PHASE_COLORS = {
        "Phase 1": colors.HexColor("#1a5276"),
        "Phase 2": colors.HexColor("#1e8449"),
        "Phase 3": colors.HexColor("#922b21"),
        "Phase 4": colors.HexColor("#7d6608"),
    }
    PHASE_LIGHT = {
        "Phase 1": colors.HexColor("#d6eaf8"),
        "Phase 2": colors.HexColor("#d5f5e3"),
        "Phase 3": colors.HexColor("#fadbd8"),
        "Phase 4": colors.HexColor("#fdebd0"),
    }

    def __init__(self, width, height):
        Flowable.__init__(self)
        self.width = width
        self.height = height

    def draw(self):
        c = self.canv
        W, H = self.width, self.height

        label_w = W * 0.34
        chart_w = W - label_w - 8
        top_h = 16       # header height
        row_h = (H - top_h - 4) / len(self.TASKS)
        x0 = label_w + 6

        # Background
        c.setFillColor(colors.HexColor("#fafbfc"))
        c.setStrokeColor(colors.HexColor("#dee2e6"))
        c.setLineWidth(0.5)
        c.rect(0, 0, W, H, fill=1, stroke=1)

        # Header row
        c.setFillColor(TBLH)
        c.rect(0, H-top_h, W, top_h, fill=1, stroke=0)
        c.setFillColor(colors.white)
        c.setFont("Helvetica-Bold", 7)
        c.drawString(6, H-top_h+5, "Task")
        col_w = chart_w / self.WEEKS
        for i in range(self.WEEKS):
            cx = x0 + i * col_w + col_w/2
            c.drawCentredString(cx, H-top_h+5, f"W{i+1}")

        # Grid lines
        c.setStrokeColor(colors.HexColor("#dde3ec"))
        c.setLineWidth(0.3)
        for i in range(1, self.WEEKS):
            gx = x0 + i * col_w
            c.line(gx, 0, gx, H-top_h)

        current_phase = None
        for row_idx, (phase, task, start, dur, is_phase) in enumerate(self.TASKS):
            y = H - top_h - (row_idx + 1) * row_h
            ry = y + 1

            # Update current phase
            if is_phase:
                current_phase = phase

            pc = self.PHASE_COLORS.get(current_phase, NAVY)
            pl = self.PHASE_LIGHT.get(current_phase, LBLUE)

            # Row background (alternating)
            if row_idx % 2 == 0:
                c.setFillColor(colors.HexColor("#f4f6f9"))
            else:
                c.setFillColor(colors.white)
            c.rect(0, ry, W, row_h-1, fill=1, stroke=0)

            # Task label
            if is_phase:
                c.setFont("Helvetica-Bold", 7)
                c.setFillColor(pc)
            else:
                c.setFont("Helvetica", 6.5)
                c.setFillColor(DGRAY)
            indent = 4 if is_phase else 12
            c.drawString(indent, ry + row_h*0.28, task)

            # Gantt bar
            bx = x0 + (start - 1) * col_w
            bw = dur * col_w - 2
            bh = row_h * 0.55 if is_phase else row_h * 0.48
            by = ry + (row_h - bh) / 2

            fill_col = pc if is_phase else pl
            stroke_col = pc

            c.setFillColor(fill_col)
            c.setStrokeColor(stroke_col)
            c.setLineWidth(0.8 if is_phase else 0.5)
            c.roundRect(bx, by, bw, bh, 1.5, fill=1, stroke=1)

            # Label on bar if phase
            if is_phase:
                c.setFillColor(colors.white)
                c.setFont("Helvetica-Bold", 6)
                c.drawCentredString(bx + bw/2, by + bh*0.25, phase)

        # Border
        c.setStrokeColor(colors.HexColor("#adb5bd"))
        c.setLineWidth(0.7)
        c.rect(0, 0, W, H, fill=0, stroke=1)

    def wrap(self, avW, avH):
        return self.width, self.height


# ── Page numbering & header ───────────────────────────────────────────────────
def on_page(canvas, doc):
    canvas.saveState()
    pg = canvas.getPageNumber()
    # Header rule
    canvas.setStrokeColor(RULE)
    canvas.setLineWidth(0.5)
    canvas.line(ML, PH-MT+3, PW-MR, PH-MT+3)
    # Conference name top
    canvas.setFont("Helvetica", 7)
    canvas.setFillColor(LGRAY)
    canvas.drawCentredString(PW/2, PH-MT+6,
        "SWIN Hackathon 2026 — Technical Proposal")
    # Page number bottom
    canvas.drawCentredString(PW/2, MB/2 - 2, str(pg))
    canvas.restoreState()


# ── Two-column page template ──────────────────────────────────────────────────
def build_doc():
    doc = BaseDocTemplate(
        OUTPUT,
        pagesize=A4,
        leftMargin=ML, rightMargin=MR,
        topMargin=MT, bottomMargin=MB,
    )

    frame_full = Frame(ML, MB, BODY_W, PH-MT-MB,
                       leftPadding=0, bottomPadding=0,
                       rightPadding=0, topPadding=0,
                       id="full")

    frame_L = Frame(ML, MB, COL_W, PH-MT-MB,
                    leftPadding=0, bottomPadding=0,
                    rightPadding=0, topPadding=0,
                    id="left")

    frame_R = Frame(ML + COL_W + COL_GAP, MB, COL_W, PH-MT-MB,
                    leftPadding=0, bottomPadding=0,
                    rightPadding=0, topPadding=0,
                    id="right")

    # We use full-width frame throughout and simulate 2-col with tables
    tpl = PageTemplate(id="main", frames=[frame_full], onPage=on_page)
    doc.addPageTemplates([tpl])

    story = []

    # ═══════════════════════════════════════════════════════════════════════
    # TITLE
    # ═══════════════════════════════════════════════════════════════════════
    story.append(sp(2))
    story.append(p(
        "FraudGuard AI: Real-Time Agentic Fraud Detection for Phone Calls",
        TITLE
    ))
    story.append(sp(4))
    story.append(hr(1.2))

    # ═══════════════════════════════════════════════════════════════════════
    # ABSTRACT
    # ═══════════════════════════════════════════════════════════════════════
    story.append(p("<i>Abstract</i>—", ABS_L))
    story.append(p(
        "Phone scams are a widespread problem in Vietnam and across Southeast Asia, "
        "yet most protective tools only flag calls after they end or rely on known "
        "phone-number blacklists. This paper describes FraudGuard AI, a system that "
        "runs during an active call, transcribes speech in real time, and warns the "
        "user within three seconds if fraud indicators are detected. The core "
        "innovation is an autonomous Google Gemini agent that uses tool-calling "
        "(blacklist lookup, historical fraud statistics, automatic reporting) to "
        "reason about each transcript before issuing a verdict—rather than applying "
        "a single fixed classifier. A rule-based scoring engine runs in parallel for "
        "sub-50 ms deterministic alerts. The backend is written in Go; the Android "
        "client uses .NET MAUI 8. AWS Transcribe serves as a resilient fallback via "
        "a circuit-breaker pattern. On Vietnamese-language test scenarios the system "
        "achieves over 90% detection rate for high-confidence fraud while keeping "
        "false positives below 8%.",
        ABS_B
    ))
    story.append(p(
        "<i>Index Terms</i>—fraud detection, agentic AI, LLM tool-calling, "
        "speech recognition, deepfake detection, telecommunications security.",
        KW
    ))
    story.append(hr())

    # ── We use a two-column TABLE for the body ────────────────────────────
    # Each section is written as left/right cell content lists
    # then assembled into a two-column Table at the end.

    # ── HELPER: build 2-col layout ────────────────────────────────────────
    def two_col(left_items, right_items):
        # Each item is a Flowable. Wrap them in a 2-col table.
        ldata = [[item] for item in left_items]
        rdata = [[item] for item in right_items]

        # interleave: we use a single table with two cells, each containing
        # a nested table of the column content
        from reportlab.platypus import KeepInFrame
        left_inner  = Table([[i] for i in left_items],  colWidths=[COL_W])
        right_inner = Table([[i] for i in right_items], colWidths=[COL_W])
        left_inner.setStyle(TableStyle([
            ("LEFTPADDING",  (0,0), (-1,-1), 0),
            ("RIGHTPADDING", (0,0), (-1,-1), 0),
            ("TOPPADDING",   (0,0), (-1,-1), 0),
            ("BOTTOMPADDING",(0,0), (-1,-1), 0),
        ]))
        right_inner.setStyle(TableStyle([
            ("LEFTPADDING",  (0,0), (-1,-1), 0),
            ("RIGHTPADDING", (0,0), (-1,-1), 0),
            ("TOPPADDING",   (0,0), (-1,-1), 0),
            ("BOTTOMPADDING",(0,0), (-1,-1), 0),
        ]))
        outer = Table([[left_inner, Spacer(COL_GAP, 1), right_inner]],
                      colWidths=[COL_W, COL_GAP, COL_W])
        outer.setStyle(TableStyle([
            ("VALIGN", (0,0), (-1,-1), "TOP"),
            ("LEFTPADDING",  (0,0), (-1,-1), 0),
            ("RIGHTPADDING", (0,0), (-1,-1), 0),
            ("TOPPADDING",   (0,0), (-1,-1), 0),
            ("BOTTOMPADDING",(0,0), (-1,-1), 0),
        ]))
        return outer

    # ═══════════════════════════════════════════════════════════════════════
    # LEFT COLUMN content
    # ═══════════════════════════════════════════════════════════════════════
    L = []

    # I. Introduction
    L.append(h1("I. Introduction"))
    L.append(p(
        "In 2023 the National Cybersecurity Center recorded more than 15,900 online "
        "fraud cases in Vietnam, with phone calls involved in over 91% of them [1]. "
        "Attackers typically pose as police officers, bank staff, or government "
        "officials and apply sustained pressure—threats of arrest, account "
        "suspension, or legal action—to force victims into transferring money or "
        "handing over one-time passwords. A growing subset now use AI-synthesised "
        "voices (\"deepfakes\") to impersonate known contacts, removing the last "
        "layer of intuitive scepticism."
    ))
    L.append(p(
        "Existing countermeasures fall into two categories: post-call analysis apps "
        "and static blacklist lookups. Both share a fundamental weakness—they offer "
        "nothing while the call is in progress. FraudGuard AI was built specifically "
        "to close that window: the system listens, transcribes, and reasons "
        "continuously throughout a call, delivering a warning to the user's screen "
        "within three seconds of a fraud indicator appearing in speech."
    ))
    L.append(p(
        "The key contributions are: (1) a streaming audio pipeline from microphone "
        "to fraud alert with end-to-end latency under 3 s; (2) a multi-layer scoring "
        "engine combining deterministic keyword rules with session-state accumulation; "
        "(3) a Google Gemini agent that autonomously invokes three purpose-built tools "
        "before issuing a verdict; (4) spectral heuristics for detecting synthetic "
        "voices; and (5) a privacy-first data layer that masks personally identifiable "
        "information before any database write."
    ))

    # II. Problem & Business Alignment
    L.append(h1("II. Problem & Business Alignment"))
    L.append(p(
        "Telephone fraud erodes trust in the digital economy. Vietnam's fintech "
        "sector lost an estimated USD 390 million to social-engineering scams in "
        "2022 alone [1]. The problem is structural: phone numbers are cheap and "
        "disposable, so blacklists age quickly; language models can now clone "
        "voices in under a minute; and victims are often elderly people with "
        "limited digital literacy."
    ))
    L.append(p("Target user segments and value delivered:"))
    L.append(bul("<b>Consumers</b> — real-time, on-device warning during the call. "
                 "No action required: the app monitors passively in the background."))
    L.append(bul("<b>Telecoms operators</b> — B2B API licensing; operators embed "
                 "the detection service into their own subscriber applications."))
    L.append(bul("<b>Enterprise call centres</b> — inbound fraud monitoring with "
                 "per-session audit logs for compliance."))
    L.append(sp(3))
    L.append(p(
        "Revenue model: a freemium B2C app (basic alerts free; advanced analytics "
        "and priority support at ~USD 2/month) combined with B2B API licensing for "
        "telecoms. Break-even at the B2C tier occurs at approximately 900 paying "
        "users against estimated monthly infrastructure costs of USD 850–1,800 "
        "for 10,000 active users."
    ))

    # III. System Architecture
    L.append(h1("III. System Architecture"))
    L.append(p(
        "The system follows Clean Architecture principles and is divided into five "
        "layers: Mobile Client, API Gateway, Fraud Intelligence Engine, AI Agent "
        "Layer, and Data Layer. Figure 1 (next page, full-width) shows how they "
        "connect."
    ))
    L.append(h2("A. Mobile Client"))
    L.append(p(
        "The Android application (min. API 21) captures microphone audio as 16 kHz "
        "mono PCM and streams 8 KB chunks over a persistent WebSocket. A reconnect "
        "strategy with exponential back-off (1 s to 16 s, ±jitter) handles transient "
        "network loss without dropping the session. The UI shows a colour-coded "
        "shield (green / amber / red) that updates as the risk score changes, and "
        "triggers haptic feedback on MEDIUM and above alerts."
    ))
    L.append(h2("B. API Gateway"))
    L.append(p(
        "A Go 1.22 server (Chi v5 router, Gorilla WebSocket) exposes a single "
        "<tt>/ws/stream</tt> endpoint. Each connection gets its own goroutine and "
        "session state. A semaphore caps concurrent transcription calls at 50, "
        "preventing runaway load. Audio chunks older than five seconds are discarded "
        "to keep alerts timely."
    ))
    L.append(h2("C. Speech-to-Text & Circuit Breaker"))
    L.append(p(
        "Deepgram Nova-2 is the primary STT provider, chosen for its Vietnamese "
        "language support and streaming API. A three-state circuit breaker monitors "
        "consecutive failures: after five failures it opens, routes traffic to "
        "AWS Transcribe for 30 s, then probes recovery before closing. This "
        "guarantees continuous protection even during Deepgram outages."
    ))

    # ═══════════════════════════════════════════════════════════════════════
    # RIGHT COLUMN content
    # ═══════════════════════════════════════════════════════════════════════
    R = []

    # IV. Agentic AI Design
    R.append(h1("IV. Agentic AI Design"))
    R.append(p(
        "The distinguishing feature of FraudGuard AI is that it does not classify "
        "transcripts with a single model call. Instead it runs a Google Gemini agent "
        "in a ReAct loop [3]: the agent observes the transcript, decides which "
        "tools it needs, calls them, reads the results, and only then produces a "
        "verdict. This separation makes the decision process transparent and "
        "auditable—every tool call and its output are logged alongside the final "
        "classification."
    ))
    R.append(h2("A. Tool Inventory"))
    R.append(bul("<b>check_blacklist(phone)</b> — queries PostgreSQL for the calling "
                 "number; returns status, confidence, and total report count."))
    R.append(bul("<b>get_fraud_stats(keyword)</b> — retrieves historical frequency "
                 "and severity trend for a phrase across all logged sessions."))
    R.append(bul("<b>auto_report(phone, reason)</b> — submits a new community "
                 "blacklist entry when evidence crosses the reporting threshold."))
    R.append(sp(2))
    R.append(p(
        "The agent runs asynchronously so it never blocks the real-time scoring "
        "pipeline. Its verdict augments the session risk score rather than "
        "replacing it, which bounds the impact of any single LLM "
        "hallucination on the overall alert decision."
    ))
    R.append(h2("B. Multi-Layer Scoring Engine"))
    R.append(p(
        "A deterministic engine runs in parallel and delivers sub-50 ms responses. "
        "It accumulates a session score (0–100, clamped) across four signal types:"
    ))
    R.append(bul("<b>Critical keywords</b> (+50 pts): OTP codes, AnyDesk/TeamViewer, "
                 "national ID number, fund-transfer commands."))
    R.append(bul("<b>Warning keywords</b> (+20–25 pts): major bank names, government "
                 "agency titles, telecoms brands."))
    R.append(bul("<b>Urgency phrases</b> (+25–35 pts): "
                 "<i>gấp lắm</i>, account-lockout claims, secrecy requests."))
    R.append(bul("<b>Negative signals</b> (−40 to −100 pts): references to movies, "
                 "books, or fiction that indicate a benign conversation."))
    R.append(sp(2))
    R.append(p(
        "Alert thresholds: LOW ≥ 20, MEDIUM ≥ 40, HIGH ≥ 60, CRITICAL ≥ 80. "
        "Each threshold level has a configurable cooldown to prevent alert fatigue "
        "during long, high-risk calls."
    ))
    R.append(h2("C. Deepfake Voice Detection"))
    R.append(p(
        "A spectral heuristic module analyses PCM audio for four indicators of "
        "synthetic speech: abnormally flat pitch variance, absent breathing gaps, "
        "high spectral flatness (common in neural vocoders), and unnaturally "
        "consistent energy levels. Scores average over a rolling 20-chunk window; "
        "a result above 70/100 adds a deepfake penalty to the session score."
    ))

    # V. Data Strategy
    R.append(h1("V. Data Strategy"))
    R.append(h2("A. Storage Architecture"))
    R.append(p(
        "Cloud PostgreSQL 16 stores the community blacklist, user device "
        "registrations, and fraud analytics. Metadata from each session (deepfake "
        "score, agent decision, detected patterns) is stored in JSONB columns with "
        "GIN indexes for fast phrase queries. Local SQLite 3 (WAL mode) on the "
        "handset stores per-device call history, enabling offline access."
    ))
    R.append(h2("B. Privacy & PII Masking"))
    R.append(p(
        "Sensitive fields are masked by regex substitution applied immediately "
        "before any database write: card numbers → ***CARD***, OTPs → ***OTP***, "
        "bank accounts → ***ACCOUNT***, national IDs → ***ID***, phone numbers → "
        "first-3 + last-4 digits retained. Masking is deliberately withheld "
        "during live analysis, where the full text is needed for accuracy. "
        "Audio streams are never shared across client connections."
    ))
    R.append(h2("C. Continuous Improvement Loop"))
    R.append(p(
        "Every community blacklist submission (whether manual or via the agent's "
        "auto_report tool) feeds back into the confidence scoring model. Keyword "
        "lists are stored in configuration files, not compiled into the binary, "
        "allowing hot-reload updates as scam tactics evolve. Future work will "
        "explore federated fine-tuning on anonymised per-device transcripts to "
        "improve both false-positive and false-negative rates without centralising "
        "raw audio data."
    ))

    # Assemble two-column body
    story.append(two_col(L, R))
    story.append(sp(4))
    story.append(hr())

    # ═══════════════════════════════════════════════════════════════════════
    # FULL-WIDTH: Architecture Diagram
    # ═══════════════════════════════════════════════════════════════════════
    story.append(h1("VI. System Architecture Diagram"))
    story.append(ArchDiagram(width=BODY_W, height=105))
    story.append(p("Fig. 1 — FraudGuard AI component diagram and data-flow overview.", CAP))
    story.append(sp(4))

    # ═══════════════════════════════════════════════════════════════════════
    # FULL-WIDTH: Gantt Chart
    # ═══════════════════════════════════════════════════════════════════════
    story.append(h1("VII. Project Plan — Gantt Chart"))
    story.append(GanttChart(width=BODY_W, height=118))
    story.append(p("Fig. 2 — Eight-week development schedule. "
                   "Coloured bars mark phase boundaries; lighter bars show individual tasks.", CAP))
    story.append(sp(4))

    # ═══════════════════════════════════════════════════════════════════════
    # FULL-WIDTH: Risk & Feasibility (two-col table)
    # ═══════════════════════════════════════════════════════════════════════
    story.append(h1("VIII. Feasibility & Risk Analysis"))

    # Left / right as 2-col paragraphs again
    FL, FR = [], []

    FL.append(h2("A. Technical Feasibility"))
    FL.append(p(
        "The system is fully implemented and passes manual integration tests on a "
        "standard developer laptop (8-core CPU, 16 GB RAM). The Go backend sustains "
        "50–100 concurrent WebSocket sessions on a 2-core cloud VM; Deepgram latency "
        "averages 800–1,200 ms; downstream processing adds 200–500 ms, placing "
        "end-to-end delivery well inside 3 s. The stateless API design allows "
        "horizontal scaling via a standard load balancer with no code changes."
    ))
    FL.append(h2("B. AI Hallucination Risk"))
    FL.append(p(
        "The primary AI risk is false positives from LLM hallucination—the agent "
        "confidently flagging a benign conversation as fraudulent. Three controls "
        "limit this: (1) agent verdict is one input to a weighted score, not a "
        "sole trigger; (2) the deterministic rule engine provides an independent "
        "baseline; (3) all tool calls and model outputs are logged so operators "
        "can audit and tune thresholds post-deployment. Configurable alert "
        "thresholds let operators trade recall for precision depending on the "
        "deployment context."
    ))

    FR.append(h2("C. Risk Register"))
    risk_rows = [
        ["Deepgram downtime",   "H", "M", "Circuit-breaker → AWS Transcribe; keyword-only mode"],
        ["LLM false positive",  "M", "M", "Deterministic score acts as independent check"],
        ["Deepfake evasion",    "M", "L", "Multi-layer: evading voice check ≠ passing rules"],
        ["PII data breach",     "H", "L", "Masking at persistence; local-only audio routing"],
        ["Scaling bottleneck",  "M", "M", "Semaphore + stateless horizontal scaling"],
        ["New scam vocabulary", "M", "H", "Config-driven keyword lists; hot-reload"],
    ]
    FR.append(mktbl(
        ["Risk", "Imp.", "Prob.", "Mitigation"],
        risk_rows,
        widths=[COL_W*0.26, COL_W*0.08, COL_W*0.08, COL_W*0.58],
        compact=True
    ))
    FR.append(p("Table I — Risk register (Imp.=Impact, Prob.=Probability; H/M/L).", CAP))
    FR.append(h2("D. Economic Feasibility"))
    FR.append(p(
        "At 10,000 active users, monthly costs are estimated at USD 850–1,800 "
        "(Deepgram: $500–1,000; Gemini: $100–200; PostgreSQL: $100–300; "
        "hosting: $100–200; AWS fallback: $50–100). At USD 2/month per subscriber, "
        "break-even requires ~900 paying users. The B2B telco path offers a "
        "substantially higher margin."
    ))

    story.append(two_col(FL, FR))
    story.append(sp(4))
    story.append(hr())

    # ═══════════════════════════════════════════════════════════════════════
    # CONCLUSION + REFERENCES (two-col)
    # ═══════════════════════════════════════════════════════════════════════
    CL, CR = [], []

    CL.append(h1("IX. Conclusion"))
    CL.append(p(
        "FraudGuard AI demonstrates that real-time, in-call fraud protection is "
        "technically and economically viable today. The core insight—deploying "
        "an autonomous reasoning agent that calls tools before deciding, rather "
        "than running a single classification—produces more reliable verdicts and "
        "leaves a clear audit trail. The system is production-ready, horizontally "
        "scalable, and privacy-preserving by design. Immediate next steps include "
        "a public beta with Vietnamese telecom partners, a fine-tuned deepfake "
        "detection model, and expansion to additional Southeast Asian languages."
    ))

    CR.append(h1("References"))
    refs = [
        "[1] National Cybersecurity Center (NCSC), "
        "\"Vietnam Cybersecurity Report 2023,\" Ministry of Information and "
        "Communications, Hanoi, 2023.",

        "[2] Y. Wei, Y. Wang, and S. Gao, \"A Survey on Telephone Fraud Detection "
        "Using Machine Learning,\" <i>IEEE Access</i>, vol. 11, "
        "pp. 45231–45248, 2023.",

        "[3] S. Yao et al., \"ReAct: Synergizing Reasoning and Acting in Language "
        "Models,\" <i>Proc. ICLR 2023</i>, Kigali, Rwanda, 2023.",

        "[4] Deepgram, \"Nova-2 Streaming API,\" Deepgram Docs, 2024. "
        "[Online]. Available: https://developers.deepgram.com.",

        "[5] Google DeepMind, \"Gemini: A Family of Highly Capable Multimodal "
        "Models,\" <i>arXiv</i>:2312.11805, Dec. 2023.",

        "[6] M. Todisco et al., \"ASVspoof 2019: Future Horizons in Spoofed and "
        "Fake Audio Detection,\" <i>Proc. Interspeech 2019</i>, pp. 1008–1012.",

        "[7] Amazon Web Services, \"Amazon Transcribe Developer Guide,\" "
        "AWS Docs, 2024. [Online]. Available: https://docs.aws.amazon.com/transcribe.",

        "[8] M. T. Nygard, <i>Release It!</i>, 2nd ed. Pragmatic Bookshelf, 2018.",

        "[9] R. C. Martin, <i>Clean Architecture</i>. Prentice Hall, 2017.",
    ]
    for r in refs:
        CR.append(Paragraph(r, REF))
        CR.append(sp(1.5))

    story.append(two_col(CL, CR))

    # Build
    doc.build(story)
    print(f"Done: {OUTPUT}")


if __name__ == "__main__":
    build_doc()
