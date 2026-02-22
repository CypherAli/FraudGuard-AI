#!/usr/bin/env python3
"""
FraudGuard AI — Technical Proposal PDF
IEEE standard double-column layout.
- Title + Abstract: full-width (single column)
- All body sections: true double-column (left fills first, then right)
- Figures 1 & 3: full-width spanning both columns
- All tables: column-width (fit inside one column)
- _col_base[0/1]: tracks where each column STARTS on the current page
  so the right column never overlaps a full-width element placed earlier.
"""
from fpdf import FPDF

# ── Page geometry ──────────────────────────────────────────────
PW, PH   = 210, 297
ML, MR   = 19.05, 19.05
MT_HDR   = 19.0            # top margin (header lives above this)
MT       = 25.4            # body top margin
MB       = 25.4
CW       = PW - ML - MR   # ≈ 171.9 mm  (full content width)
BOTTOM   = PH - MB        # ≈ 271.6 mm
COL_GAP  = 4.22
COL_W    = (CW - COL_GAP) / 2   # ≈ 83.84 mm
COL_X    = [ML, ML + COL_W + COL_GAP]

SZ_TIT  = 16
SZ_ABST = 8.5
SZ_BODY = 9
SZ_SECT = 9.5
SZ_SM   = 7.5
SZ_XS   = 6.5
SZ_REF  = 7.5
ROMAN   = ["I","II","III","IV","V","VI","VII","VIII","IX","X"]
FONT_DIR = "C:/Windows/Fonts/"
FONTS    = [("F","","arial.ttf"),("F","B","arialbd.ttf"),
            ("F","I","ariali.ttf"),("F","BI","arialbi.ttf")]


# ═══════════════════════════════════════════════════════════════
class Paper(FPDF):
    def __init__(self):
        super().__init__("P","mm","A4")
        self.set_auto_page_break(False)
        for n,s,f in FONTS:
            self.add_font(n, s, FONT_DIR+f)
        self._sec           = 0
        self._col           = 0       # 0=left, 1=right
        self._twocol        = False
        self._y             = MT
        # _col_base[i] = y where column i starts on this page.
        # After a full-width block, both bases are reset so the right
        # column never starts above (and overlaps) that block.
        self._col_base      = [MT, MT]
        # _sep_seg_start: where the current separator segment begins on
        # the current page.  We draw lazily in segments so the line
        # is never painted across full-width figures.
        self._sep_seg_start = MT

    # ── column-aware getters ───────────────────────────────────
    @property
    def cx(self):
        return COL_X[self._col] if self._twocol else ML
    @property
    def cw(self):
        return COL_W if self._twocol else CW

    # ── space helpers ──────────────────────────────────────────
    def sp(self, mm=2):
        self._y += mm

    def remain(self):
        return BOTTOM - self._y

    def need(self, h):
        if self._y + h > BOTTOM:
            if self._twocol and self._col == 0:
                # Switch to right column; start it at its saved base
                self._col = 1
                self._y   = self._col_base[1]
            else:
                self._np()

    def _np(self):
        """New page (resets both column bases)."""
        # Close the current separator segment on the page we're leaving
        if self._twocol:
            self._draw_sep_seg(self._sep_seg_start, BOTTOM)
        self.add_page()
        self._col           = 0
        self._y             = MT
        self._col_base      = [MT, MT]
        self._sep_seg_start = MT   # New page: sep segment starts at top

    def start_twocol(self, y_base=None):
        """Enter 2-column mode. y_base = where both columns start."""
        if y_base is None:
            y_base = self._y
        self._twocol        = True
        self._col           = 0
        self._col_base      = [y_base, y_base]
        self._y             = y_base
        self._sep_seg_start = y_base   # Lazy: draw segment when needed

    def _draw_sep_seg(self, y_top, y_bot):
        """Draw one segment of the thin vertical rule between columns.
        Segments are accumulated lazily so the line is never painted
        across full-width figures or tables."""
        if y_bot <= y_top + 0.5:   # nothing to draw
            return
        sep_x = ML + COL_W + COL_GAP / 2
        self.set_draw_color(160,160,160)
        self.set_line_width(0.15)
        self.line(sep_x, y_top, sep_x, y_bot)
        self.set_draw_color(0,0,0)

    # ── header / footer ────────────────────────────────────────
    def header(self):
        self.set_font("F","B",7.5)
        self.set_text_color(60,60,60)
        self.set_xy(ML,10)
        self.cell(CW/2,5,"SWIN Hackathon 2026",align="L")
        self.set_font("F","",7.5)
        self.set_xy(ML+CW/2,10)
        self.cell(CW/2,5,"Technical Proposal \u2014 FraudGuard AI",align="R")
        self.set_draw_color(0,90,140)
        self.set_line_width(0.5)
        self.line(ML,17,PW-MR,17)
        self.set_text_color(0,0,0)

    def footer(self):
        self.set_y(-15)
        self.set_font("F","",7.5)
        self.set_text_color(100,100,100)
        self.cell(0,10,f"- {self.page_no()} -",align="C")
        self.set_text_color(0,0,0)

    # ── text helpers ───────────────────────────────────────────
    def tx(self, txt, style="", sz=SZ_BODY, lh=None, align="J", ind=0):
        if lh is None: lh = sz*0.46
        self.set_font("F",style,sz)
        self.set_xy(self.cx+ind, self._y)
        self.multi_cell(self.cw-ind, lh, txt, align=align,
                        new_x="LEFT", new_y="NEXT")
        self._y = self.get_y()

    def md(self, txt, sz=SZ_BODY, lh=None, align="J", ind=0):
        if lh is None: lh = sz*0.46
        self.set_font("F","",sz)
        self.set_xy(self.cx+ind, self._y)
        self.multi_cell(self.cw-ind, lh, txt, align=align, markdown=True,
                        new_x="LEFT", new_y="NEXT")
        self._y = self.get_y()

    def bul(self, txt, sz=SZ_BODY, ind=4):
        lh = sz*0.46
        self.set_font("F","",sz)
        self.set_xy(self.cx+1.5, self._y)
        self.cell(2.5, lh, "\u2022")
        self.set_xy(self.cx+ind, self._y)
        self.multi_cell(self.cw-ind, lh, txt, align="J", markdown=True,
                        new_x="LEFT", new_y="NEXT")
        self._y = self.get_y()

    def sec(self, title):
        self._sec += 1
        num = ROMAN[self._sec-1]
        self.need(12)
        y = self._y
        # Subtle background strip
        self.set_fill_color(232, 242, 252)
        self.rect(self.cx, y, self.cw, 8.5, "F")
        # Bold left accent bar
        self.set_fill_color(0, 90, 140)
        self.rect(self.cx, y, 4, 8.5, "F")
        self.set_font("F","B",SZ_SECT)
        self.set_text_color(0, 45, 100)
        self.set_xy(self.cx+6, y+0.7)
        self.cell(self.cw-6, 7.5, f"{num}. {title.upper()}", align="L")
        self.set_text_color(0,0,0)
        self._y += 11

    def sub(self, txt):
        self.need(8)
        # Subtle underline sub-heading
        self.set_font("F","BI",SZ_BODY)
        self.set_text_color(0,60,120)
        self.set_xy(self.cx, self._y)
        self.cell(self.cw, 5, txt)
        self.set_draw_color(0,90,140)
        self.set_line_width(0.2)
        self.line(self.cx, self._y+5.2, self.cx+self.cw, self._y+5.2)
        self.set_draw_color(0,0,0)
        self.set_text_color(0,0,0)
        self._y += 6.5

    def caption(self, txt, fullw=False):
        w = CW if fullw else self.cw
        x = ML if fullw else self.cx
        # Light grey background for caption
        self.set_fill_color(245,245,245)
        self.rect(x, self._y, w, 5, "F")
        self.set_font("F","I",SZ_SM-0.5)
        self.set_text_color(70,70,70)
        self.set_xy(x, self._y+0.3)
        self.cell(w, 4.5, txt, align="C")
        self.set_text_color(0,0,0)
        self._y += 6

    def callout(self, txt, sz=SZ_SM, bg=(236,245,255), accent=(0,90,140)):
        """Highlighted info box — for key metrics, thresholds, etc."""
        lh = sz*0.46
        self.set_font("F","",sz)
        n = len(self.multi_cell(self.cw-7, lh, txt, dry_run=True, output="LINES"))
        h = n*lh + 4
        self.need(h+2)
        y = self._y
        r,g,b = bg; ar,ag,ab = accent
        self.set_fill_color(r,g,b)
        self.set_draw_color(ar,ag,ab)
        self.set_line_width(0.25)
        self.rect(self.cx, y, self.cw, h, "FD")
        self.set_fill_color(ar,ag,ab)
        self.rect(self.cx, y, 2.5, h, "F")
        self.set_font("F","",sz)
        self.set_text_color(20,20,20)
        self.set_xy(self.cx+4.5, y+2)
        self.multi_cell(self.cw-6, lh, txt, align="J",
                        new_x="LEFT", new_y="NEXT")
        self.set_text_color(0,0,0)
        self._y = y+h+2

    # ── table (column-aware, handles mid-table col/page breaks) ─
    def tbl(self, hdrs, rows, widths, cap="", sz=None):
        if sz is None: sz = SZ_SM
        rh = sz*0.65
        lh = sz*0.58
        x0 = self.cx

        def draw_hdr(yp):
            self.set_fill_color(0,80,130)
            self.set_text_color(255,255,255)
            self.set_draw_color(0,60,110)
            self.set_font("F","B",sz)
            x = x0
            for i,h in enumerate(hdrs):
                self.set_xy(x,yp)
                self.cell(widths[i],rh+0.5,h,border=1,align="C",fill=True)
                x += widths[i]
            self.set_text_color(0,0,0)
            self.set_draw_color(0,0,0)
            return yp+rh+0.5

        y = self._y
        y = draw_hdr(y)
        self.set_font("F","",sz)

        for ri,row in enumerate(rows):
            mh = rh
            for ci,ct in enumerate(row):
                n = len(self.multi_cell(widths[ci]-0.5,lh,ct,
                                        dry_run=True,output="LINES"))
                mh = max(mh, n*lh+0.8)

            if y + mh > BOTTOM - 2:
                if self._twocol and self._col == 0:
                    # Switch to right column
                    self._col = 1
                    self._y   = self._col_base[1]
                    x0        = self.cx
                    y         = draw_hdr(self._y)
                    self.set_font("F","",sz)
                else:
                    self._np()
                    x0 = self.cx
                    y  = draw_hdr(self._y)
                    self.set_font("F","",sz)

            fill = ri%2==0
            self.set_draw_color(180,200,220)
            self.set_line_width(0.15)
            if fill: self.set_fill_color(241,247,254)
            else:    self.set_fill_color(255,255,255)
            x = x0
            for ci,ct in enumerate(row):
                self.set_xy(x,y)
                self.cell(widths[ci],mh,"",border=1,fill=True)
                self.set_xy(x+0.3,y+0.3)
                self.multi_cell(widths[ci]-0.6,lh,ct,align="C")
                x += widths[ci]
            self.set_draw_color(0,0,0)
            self.set_line_width(0.2)
            y += mh

        self._y = y
        if cap: self.caption(cap)

    # ── full-width figure (spans both columns) ─────────────────
    def fw(self, draw_fn, h, cap=""):
        """
        Draw a full-width element (figure) spanning BOTH columns.
        Forces a new page if there isn't enough room.
        After drawing, resets _col_base so the right column starts
        BELOW the figure — preventing any future content overlap.
        The separator segment is closed above the figure and a new
        segment is opened below it, so the rule never crosses figures.
        """
        needed = h + (6 if cap else 0)
        if self._y + needed > BOTTOM:
            self._np()

        # Close the separator segment just above the figure's top edge
        if self._twocol:
            self._draw_sep_seg(self._sep_seg_start, self._y)

        # Temporarily go single-column / full-width
        saved_twocol = self._twocol
        self._twocol = False
        self._col    = 0

        draw_fn(self, ML, self._y, CW)

        if cap:
            self.set_font("F","I",SZ_SM-0.5)
            self.set_text_color(80,80,80)
            self.set_xy(ML, self._y)
            self.cell(CW, 4.5, cap, align="C")
            self.set_text_color(0,0,0)
            self._y += 5.5

        # Restore 2-col; BOTH columns must start below the figure
        self._twocol        = saved_twocol
        self._col           = 0
        self._col_base      = [self._y, self._y]
        # New separator segment starts immediately below the figure
        if self._twocol:
            self._sep_seg_start = self._y


# ═══════════════════════════════════════════════════════════════
# FIGURE 1 — Component diagram (full-width)
# ═══════════════════════════════════════════════════════════════
def fig1(pdf, x0, y0, W):
    BOX_W = (W - 2*22) / 3
    BOX_H = 14
    GAP_X = 22
    GAP_Y = 14
    col_x = [x0, x0+BOX_W+GAP_X, x0+2*(BOX_W+GAP_X)]
    row_y = [y0, y0+BOX_H+GAP_Y, y0+2*(BOX_H+GAP_Y)]
    COLORS = {"dark":(44,62,80),"teal":(0,128,128),
              "green":(46,139,87),"brown":(139,90,43)}
    boxes = [
        (0,0,"Mobile Client","(.NET MAUI 8 / Android)","dark"),
        (1,0,"API Gateway","(Go 1.22 / Chi v5)","dark"),
        (2,0,"PostgreSQL 16","(Cloud DB)","dark"),
        (0,1,"Deepgram Nova-2","(Primary STT)","teal"),
        (1,1,"Fraud Engine","(Rule + Session Score)","teal"),
        (2,1,"Gemini Agent","(ReAct + 3 Tools)","brown"),
        (0,2,"AWS Transcribe","(Circuit-Breaker Fallback)","green"),
        (1,2,"SQLite 3 Local","(Call History / WAL)","green"),
        (2,2,"Blacklist DB","(Community Reports)","brown"),
    ]
    for ci,ri,t1,t2,ck in boxes:
        bx,by = col_x[ci],row_y[ri]
        r,g,b = COLORS[ck]
        pdf.set_fill_color(r,g,b)
        pdf.set_draw_color(max(0,r-30),max(0,g-30),max(0,b-30))
        pdf.set_line_width(0.2)
        pdf.rect(bx,by,BOX_W,BOX_H,"FD")
        pdf.set_text_color(255,255,255)
        pdf.set_font("F","B",7.5)
        pdf.set_xy(bx,by+1.5); pdf.cell(BOX_W,BOX_H*0.4,t1,align="C")
        pdf.set_font("F","",6)
        pdf.set_xy(bx,by+BOX_H*0.45); pdf.cell(BOX_W,BOX_H*0.4,t2,align="C")
        pdf.set_text_color(0,0,0)
    pdf.set_draw_color(90,90,90); pdf.set_line_width(0.35)
    def arr(x2,y2,d="right"):
        s=1.5
        if d=="right":
            pdf.line(x2-s,y2-s*0.7,x2,y2); pdf.line(x2-s,y2+s*0.7,x2,y2)
        else:
            pdf.line(x2-s*0.7,y2-s,x2,y2); pdf.line(x2+s*0.7,y2-s,x2,y2)
    for fc,ri,lbl,_ in [(0,0,"WebSocket (PCM audio)",True),(1,0,"pgx/v5",True),
                         (0,1,"transcript",True),(1,1,"context + score",True)]:
        x1=col_x[fc]+BOX_W; x2=col_x[fc+1]
        ay=row_y[ri]+BOX_H/2
        pdf.line(x1,ay,x2,ay); arr(x2,ay,"right")
        pdf.set_font("F","",6); pdf.set_text_color(60,60,60)
        pdf.set_xy(x1,ay-4.5); pdf.cell(x2-x1,4,lbl,align="C")
        pdf.set_text_color(0,0,0)
    for ci,fr,lbl,_ in [(0,0,"16 kHz PCM",True),(1,0,"transcript",True),
                         (2,0,"context",True),(0,1,"failover",True),
                         (1,1,"save log",True),(2,1,"check/report",True)]:
        ax=col_x[ci]+BOX_W/2
        y1=row_y[fr]+BOX_H; y2=row_y[fr+1]
        pdf.line(ax,y1,ax,y2); arr(ax,y2,"down")
        pdf.set_font("F","",5.5); pdf.set_text_color(60,60,60)
        pdf.set_xy(ax+1.5,(y1+y2)/2-2); pdf.cell(18,4,lbl,align="L")
        pdf.set_text_color(0,0,0)
    bot_y = row_y[2]+BOX_H+3
    pdf.set_font("F","",5.5); pdf.set_text_color(80,80,80)
    pdf.set_xy(x0,bot_y)
    pdf.cell(W,3.5,"End-to-end < 3 s  |  Circuit-breaker: Deepgram \u2192 AWS  |  "
             "Agent tools: check_blacklist, get_fraud_stats, auto_report",align="C")
    pdf.set_text_color(0,0,0)
    pdf._y = bot_y+5


# ═══════════════════════════════════════════════════════════════
# FIGURE 2 — ReAct Loop (column-width)
# ═══════════════════════════════════════════════════════════════
def fig2(pdf, x0, y0, W):
    bw=W/6.2; bh=12; gap=(W-5*bw)/4
    steps=[("OBSERVE","Parse transcript\n& session ctx",(0,90,140)),
           ("REASON","Select tools\nneeded",(0,90,140)),
           ("ACT","Execute tools\n(1-3 calls)",(0,130,130)),
           ("SYNTH.","Aggregate\n=> verdict",(0,130,130)),
           ("ALERT","Push to mobile\n(WebSocket)",(180,60,30))]
    for i,(t,sub,col) in enumerate(steps):
        bx=x0+i*(bw+gap); r,g,b=col
        pdf.set_fill_color(r,g,b); pdf.rect(bx,y0,bw,bh,"F")
        pdf.set_text_color(255,255,255)
        pdf.set_font("F","B",6); pdf.set_xy(bx,y0+1); pdf.cell(bw,bh*0.38,t,align="C")
        pdf.set_font("F","",4.8)
        for j,ln in enumerate(sub.split("\n")):
            pdf.set_xy(bx,y0+bh*0.38+j*bh*0.28); pdf.cell(bw,bh*0.28,ln,align="C")
        if i<4:
            ax1,ax2=bx+bw,bx+bw+gap; ay=y0+bh/2
            pdf.set_draw_color(80,80,80); pdf.set_line_width(0.3)
            pdf.line(ax1,ay,ax2,ay)
            pdf.line(ax2-1.5,ay-1,ax2,ay); pdf.line(ax2-1.5,ay+1,ax2,ay)
    pdf.set_text_color(0,0,0)
    act_cx=x0+2*(bw+gap)+bw/2; rsn_cx=x0+1*(bw+gap)+bw/2; lp=y0-4
    pdf.set_draw_color(130,130,130); pdf.set_line_width(0.2)
    pdf.set_dash_pattern(1,1)
    pdf.line(act_cx,y0,act_cx,lp); pdf.line(act_cx,lp,rsn_cx,lp); pdf.line(rsn_cx,lp,rsn_cx,y0)
    pdf.set_dash_pattern()
    pdf.set_font("F","I",5); pdf.set_text_color(100,100,100)
    pdf.set_xy(rsn_cx,lp-3.5); pdf.cell(act_cx-rsn_cx,3.5,"if more tools needed",align="C")
    act_x=x0+2*(bw+gap); ty=y0+bh+1.5
    pdf.set_font("F","",5); pdf.set_text_color(80,80,80)
    for i,t in enumerate(["check_blacklist(phone_number)",
                           "get_fraud_stats(keyword)",
                           "auto_report(phone_number, reason)"]):
        pdf.set_xy(act_x-6,ty+i*2.8); pdf.cell(bw+12,2.8,t,align="C")
    pdf.set_text_color(0,0,0)
    pdf._y = ty+10


# ═══════════════════════════════════════════════════════════════
# FIGURE 3 — Gantt chart (full-width)
# ═══════════════════════════════════════════════════════════════
def fig3(pdf, x0, y0, W):
    lw=W*0.36; cw=W-lw; wk=cw/8; rh=4.2; hh=6; y=y0
    pdf.set_fill_color(44,62,80); pdf.set_text_color(255,255,255)
    pdf.set_font("F","B",7)
    pdf.rect(x0,y,lw,hh,"F"); pdf.set_xy(x0,y); pdf.cell(lw,hh,"Task",align="C")
    for i in range(8):
        x=x0+lw+i*wk; pdf.rect(x,y,wk,hh,"F")
        pdf.set_xy(x,y); pdf.cell(wk,hh,f"W{i+1}",align="C")
    y+=hh; pdf.set_text_color(0,0,0)
    items=[
        ("Phase 1: Architecture & Setup",True, (52,152,219),0,  2),
        ("  Backend (Go/Chi/WebSocket)",  False,(52,152,219),0,  1.8),
        ("  Mobile MAUI scaffold",        False,(52,152,219),0.2,1.8),
        ("  PostgreSQL schema + auth",    False,(52,152,219),0.4,1.6),
        ("Phase 2: Core Pipeline",        True, (46,204,113),2,  2.5),
        ("  Deepgram STT integration",    False,(46,204,113),2,  2),
        ("  Fraud scoring engine",        False,(46,204,113),2.3,2.2),
        ("  WebSocket real-time stream",  False,(46,204,113),2.5,2),
        ("Phase 3: AI & Intelligence",    True, (231,76,60), 4,  2.5),
        ("  Gemini agent + 3 tools",      False,(231,76,60), 4,  2.5),
        ("  Deepfake detection (spectral)",False,(231,76,60),4.3,2),
        ("  Circuit breaker + AWS fallback",False,(231,76,60),4.5,2),
        ("Phase 4: Hardening & Delivery", True, (241,196,15),6,  2),
        ("  PII masking + privacy layer", False,(241,196,15),6,  1.5),
        ("  Security hardening + bug-fix",False,(241,196,15),6.2,1.8),
        ("  Documentation & submission",  False,(241,196,15),6.5,1.5),
    ]
    for lbl,isp,(r,g,b),st,dur in items:
        if isp: pdf.set_font("F","B",6); pdf.set_text_color(r,g,b)
        else:   pdf.set_font("F","",5.5); pdf.set_text_color(50,50,50)
        pdf.set_xy(x0,y); pdf.cell(lw,rh,lbl,align="L")
        bx=x0+lw+st*wk; bw_=dur*wk
        if isp:
            pdf.set_fill_color(r,g,b); pdf.rect(bx,y+0.4,bw_,rh-0.8,"F")
            pdf.set_text_color(255,255,255); pdf.set_font("F","B",5.5)
            pdf.set_xy(bx,y+0.4); pdf.cell(bw_,rh-0.8,lbl.split(":")[0],align="C")
        else:
            pdf.set_fill_color(min(255,r+70),min(255,g+70),min(255,b+70))
            pdf.rect(bx,y+0.4,bw_,rh-0.8,"F")
        y+=rh
    pdf.set_text_color(0,0,0); pdf._y=y+1


# ═══════════════════════════════════════════════════════════════
# BUILD
# ═══════════════════════════════════════════════════════════════
def build(out):
    p = Paper()
    p.set_margins(ML, MT, MR)

    # ── PAGE 1: Title + Abstract (full-width) ─────────────────
    p.add_page(); p._sec=0; p._y=MT

    p.set_font("F","B",SZ_TIT)
    p.set_xy(ML,p._y)
    p.multi_cell(CW,7.5,
        "FraudGuard AI: Real-Time Agentic Fraud Detection for Phone Calls",
        align="C")
    p._y=p.get_y()+6    # extra gap: title → abstract (no author line)

    # Abstract
    p.set_font("F","B",SZ_BODY); p.set_xy(ML,p._y)
    p.cell(CW,5,"Abstract\u2014",align="C"); p._y+=5
    p.tx(
        "Phone scams are a critical problem in Vietnam: over 15,900 fraud cases were recorded "
        "in 2023, with 91% involving phone calls. Existing tools only act post-call or depend "
        "on static blacklists. FraudGuard AI monitors an active call in real time \u2014 it streams "
        "audio, transcribes speech, and pushes an alert to the user\u2019s phone within three "
        "seconds of a fraud indicator appearing. The core innovation is a Google Gemini agent "
        "operating in a ReAct loop with three dedicated tools: blacklist lookup, fraud-statistics "
        "retrieval, and auto-reporting. A deterministic rule-based engine runs in parallel for "
        "sub-50 ms responses. AWS Transcribe is integrated as a resilient fallback via a "
        "circuit-breaker. On Vietnamese-language test cases: >90% detection rate, <8% false "
        "positives.",
        style="I", sz=SZ_ABST
    )
    p.sp(2)
    p.tx(
        "Index Terms \u2014 agentic AI, LLM tool-calling, real-time fraud detection, "
        "speech recognition, deepfake voice analysis, telecommunications security.",
        style="I", sz=SZ_ABST-0.5
    )
    p.sp(3)

    # Separator between abstract and 2-col body
    p.set_draw_color(0,0,0); p.set_line_width(0.4)
    p.line(ML, p._y, PW-MR, p._y)
    p.sp(4)
    abst_end = p._y   # remember where abstract ends

    # ── Enter double-column mode ───────────────────────────────
    p.start_twocol(y_base=abst_end)

    # ══════════════════════════════════════════════════════════
    # I. INTRODUCTION
    # ══════════════════════════════════════════════════════════
    p.sec("INTRODUCTION")
    p.tx(
        "In Vietnam, the National Cybersecurity Center (NCSC) recorded 15,900+ online fraud "
        "cases in 2023, with phone calls involved in 91% of incidents [1]. Attackers impersonate "
        "police, banks, or government agencies, applying urgency and threats to force victims to "
        "transfer money or share OTP codes. A growing subset uses AI-generated (deepfake) voices "
        "to clone known contacts."
    )
    p.sp(2)
    p.tx(
        "Existing countermeasures fall into two categories: post-call analysis apps and static "
        "phone-number blacklists. Both share the same critical flaw \u2014 they offer zero "
        "protection while the call is still active. FraudGuard AI was built to close exactly "
        "that gap."
    )
    p.sp(2)
    p.md("**Key contributions:**")
    p.sp(1)
    p.bul("**Streaming audio pipeline:** mic \u2192 fraud alert in **<3 s** end-to-end.")
    p.bul("**Multi-layer risk scoring:** deterministic keyword rules + session accumulation.")
    p.bul("**Gemini ReAct agent** with 3 purpose-built tools for contextual reasoning.")
    p.bul("**Spectral heuristics** for deepfake voice detection.")
    p.bul("**Privacy-first:** PII masked before any database write.")
    p.sp(3)

    # ══════════════════════════════════════════════════════════
    # II. PROBLEM & BUSINESS ALIGNMENT
    # ══════════════════════════════════════════════════════════
    p.sec("PROBLEM & BUSINESS ALIGNMENT")
    p.md(
        "Vietnam\u2019s digital economy lost an estimated **USD 390 million** to social-engineering "
        "scams in 2022 [1]. The structural challenge: phone numbers are cheap and disposable "
        "(blacklists expire quickly), and AI can now clone a voice in under a minute."
    )
    p.sp(2)
    p.md("**Target users and value delivered:**")
    p.sp(1)
    p.bul("**Consumers** \u2014 passive on-device warning during the call; no action required.")
    p.bul("**Telecoms operators** \u2014 B2B API licensing; embed detection into subscriber apps.")
    p.bul("**Enterprise call centres** \u2014 inbound fraud monitoring with per-session audit logs.")
    p.sp(2)
    p.tx(
        "Revenue model: Freemium B2C (~USD\u00a02/month premium) plus B2B API licensing. "
        "Break-even at ~900 paying users vs. monthly costs USD\u00a0850\u20131,800 for 10,000 "
        "active users (Deepgram $500\u20131,000 + Gemini $100\u2013200 + infra $250\u2013600)."
    )
    p.sp(3)

    # ══════════════════════════════════════════════════════════
    # III. PROJECT REQUIREMENTS
    # ══════════════════════════════════════════════════════════
    p.sec("PROJECT REQUIREMENTS")
    p.tx(
        "Requirements are derived from B2C mobile and B2B telecom API deployment contexts. "
        "All requirements are testable and traceable to system components."
    )
    p.sp(2)

    # A. Functional Requirements (column-width)
    p.sub("A. Functional Requirements")
    p.sp(1)
    W1 = COL_W
    p.tbl(
        ["ID","Requirement","Description"],
        [
            ["FR-1","Real-Time Voice Streaming",
             "Capture 16 kHz PCM audio; stream to backend via WebSocket (<200 ms/chunk)."],
            ["FR-2","Speech-to-Text",
             "Transcribe Vietnamese with Deepgram Nova-2; auto-failover to AWS Transcribe."],
            ["FR-3","Keyword & Session Scoring",
             "Score each chunk against weighted rules; maintain cumulative score 0\u2013100."],
            ["FR-4","Autonomous Agent",
             "Gemini ReAct calls check_blacklist, get_fraud_stats, auto_report autonomously."],
            ["FR-5","Instant Alert",
             "Push colour-coded alert (LOW/MEDIUM/HIGH/CRITICAL) via WebSocket in <3 s."],
            ["FR-6","Community Blacklist",
             "Agent submits confirmed fraudster numbers to shared PostgreSQL blacklist."],
            ["FR-7","Offline Call History",
             "Persist transcripts, scores, verdicts to local SQLite 3 (WAL mode)."],
        ],
        [W1*0.09, W1*0.27, W1*0.64],
        "Table III \u2014 Functional requirements.",
        sz=SZ_XS
    )
    p.sp(2)

    # B. Non-Functional Requirements (column-width)
    p.sub("B. Non-Functional Requirements")
    p.sp(1)
    p.tbl(
        ["ID","Requirement","Target","Rationale"],
        [
            ["NFR-1","E2E Latency","< 3 s",
             "Deepgram ~1 s + proc ~0.5 s + net ~0.3 s."],
            ["NFR-2","STT Availability","\u2265 99.5%",
             "Circuit-breaker: Deepgram \u2192 AWS; zero manual ops."],
            ["NFR-3","Concurrent","\u2265 50/node",
             "Semaphore-bounded pool; stateless horizontal scaling."],
            ["NFR-4","PII Protection","Zero plaintext",
             "Regex masking before every DB write; audio never persisted."],
            ["NFR-5","False-Positive","< 8%",
             "Dual-layer cross-validation; negative-signal scoring."],
            ["NFR-6","Deepfake","Score > 70",
             "20-chunk rolling window: pitch, breath gaps, spectral flatness."],
            ["NFR-7","Keyword Adapt","Hot-reload <5 s",
             "Config-file lists reloaded at runtime; agent adapts via LLM."],
        ],
        [W1*0.09, W1*0.25, W1*0.18, W1*0.48],
        "Table IV \u2014 Non-functional requirements.",
        sz=SZ_XS
    )
    p.sp(3)

    # ══════════════════════════════════════════════════════════
    # IV. SYSTEM ARCHITECTURE
    # ══════════════════════════════════════════════════════════
    p.sec("SYSTEM ARCHITECTURE")
    p.tx(
        "FraudGuard AI uses Clean Architecture with five layers: Mobile Client, API Gateway, "
        "Fraud Intelligence Engine, AI Agent Layer, and Data Layer. Figure\u00a01 shows the "
        "full component diagram with actual data-flow arrows."
    )
    p.sp(2)

    # Figure 1 — full-width component diagram
    p.fw(fig1, 75,
         "Figure 1 \u2014 FraudGuard AI component diagram. Arrows show actual data flow.")
    p.sp(2)

    # Table I — technology stack (column-width, left column)
    p.need(48)
    p.tbl(
        ["Layer","Technology","Responsibility"],
        [
            ["Mobile Client",".NET MAUI 8","Audio capture, UI, alerts"],
            ["API Gateway","Go 1.22 / Chi v5","WebSocket, session mgmt"],
            ["Primary STT","Deepgram Nova-2","Streaming STT (Vietnamese)"],
            ["Fallback STT","AWS Transcribe","Circuit-breaker failover"],
            ["Fraud Engine","Go (custom)","Rule scoring + accumulation"],
            ["AI Agent","Google Gemini","ReAct loop + 3 tool-calls"],
            ["Cloud DB","PostgreSQL 16","Blacklist, analytics, JSONB"],
            ["Local DB","SQLite 3 WAL","On-device call history"],
        ],
        [COL_W*0.24, COL_W*0.30, COL_W*0.46],
        "Table I \u2014 Technology stack.",
        sz=SZ_XS
    )
    p.sp(3)

    # ══════════════════════════════════════════════════════════
    # V. AGENTIC AI DESIGN
    # ══════════════════════════════════════════════════════════
    p.sec("AGENTIC AI DESIGN")
    p.md(
        "The core innovation is a **Google Gemini agent in a ReAct loop** [3]. Unlike a "
        "single-call classifier, the agent autonomously reasons through each conversation turn, "
        "selects tools, calls them, reads results, then issues a verdict \u2014 without human "
        "intervention. Every tool call and result is logged; decisions are fully auditable."
    )
    p.sp(2)

    # Figure 2 — column-width ReAct loop
    p.need(36)
    fig2(p, p.cx, p._y+5, p.cw)
    p.caption("Figure 2 \u2014 ReAct loop. Dashed = optional multi-tool iteration.")
    p.sp(2)

    p.need(38)
    p.md("**The three agent tools:**")
    p.sp(1)
    p.bul("**check_blacklist(phone_number)** \u2014 queries PostgreSQL for known fraudster "
          "numbers; returns status, confidence, and report count.")
    p.bul("**get_fraud_stats(keyword)** \u2014 retrieves historical frequency and severity "
          "trend for any phrase across all logged sessions.")
    p.bul("**auto_report(phone_number, reason)** \u2014 submits a new community blacklist "
          "entry autonomously when evidence is sufficient.")
    p.sp(2)
    p.tx(
        "The agent runs asynchronously \u2014 it never blocks the real-time pipeline. Its verdict "
        "is one weighted input to the session score. This bounds the impact of any single LLM "
        "hallucination: if the agent misfires, the deterministic engine provides an independent "
        "check."
    )
    p.sp(2)
    # keep the 4 scoring bullets + callout together — need ~50mm
    p.need(52)
    p.md("**Multi-layer scoring (parallel, <50\u00a0ms):**")
    p.sp(1)
    p.bul("**Critical keywords (+50 pts):** OTP codes, AnyDesk/TeamViewer, fund-transfer "
          "phrases, national ID requests.")
    p.bul("**Warning keywords (+20\u201325 pts):** major bank names, government agencies, "
          "telecom brands.")
    p.bul("**Urgency phrases (+25\u201335 pts):** account-lockout claims, secrecy requests.")
    p.bul("**Negative signals (\u221240 to \u2212100 pts):** movie/fiction references "
          "indicating benign context.")
    p.sp(2)
    p.callout(
        "Score clamped 0\u2013100.  Alert thresholds:\n"
        "LOW \u2265 20  |  MEDIUM \u2265 40  |  HIGH \u2265 60  |  CRITICAL \u2265 80",
        sz=SZ_SM, bg=(236,245,255), accent=(0,90,140)
    )
    p.sp(2)
    p.md(
        "**Deepfake voice detection** \u2014 spectral heuristics on PCM audio: pitch variance, "
        "absent breathing gaps, high spectral flatness, over-consistent energy. Averaged over a "
        "20-chunk rolling window; >70/100 adds a deepfake penalty to session score."
    )
    p.sp(3)

    # ══════════════════════════════════════════════════════════
    # VI. DATA STRATEGY
    # ══════════════════════════════════════════════════════════
    p.sec("DATA STRATEGY")
    p.md(
        "**Storage:** Cloud PostgreSQL\u00a016 stores the community blacklist, device "
        "registrations, and per-session fraud analytics in JSONB columns (GIN-indexed for fast "
        "phrase queries). Local SQLite\u00a03 (WAL mode) on the handset stores call history offline."
    )
    p.sp(2)
    p.md(
        "**PII masking:** Applied by regex immediately before any DB write \u2014 card numbers "
        "\u2192 [CARD], OTPs \u2192 [OTP], accounts \u2192 [ACCOUNT], national IDs \u2192 [ID]. "
        "Masking is NOT applied during live analysis. Audio is never shared across client "
        "connections."
    )
    p.sp(2)
    p.md(
        "**Community improvement loop:** Every blacklist submission \u2014 manual or via the "
        "agent\u2019s auto_report tool \u2014 updates confidence scores. Keyword lists are "
        "config-file driven and hot-reloadable as scam tactics evolve. Future work: federated "
        "fine-tuning on anonymised per-device transcripts without centralising raw audio."
    )
    p.sp(3)

    # ══════════════════════════════════════════════════════════
    # VII. PROJECT PLAN — GANTT CHART
    # ══════════════════════════════════════════════════════════
    p.sec("PROJECT PLAN \u2014 GANTT CHART")

    # Figure 3 — full-width Gantt
    p.fw(fig3, 74,
         "Figure 3 \u2014 Eight-week schedule. Solid = phase milestones; lighter = tasks.")
    p.sp(2)

    # ══════════════════════════════════════════════════════════
    # VIII. FEASIBILITY & RISK ANALYSIS
    # ══════════════════════════════════════════════════════════
    p.sec("FEASIBILITY & RISK ANALYSIS")
    p.md(
        "**Technical feasibility:** The full system is implemented and manually tested. "
        "The Go backend sustains 50\u2013100 concurrent WebSocket sessions on a 2-core VM; "
        "Deepgram latency averages 800\u20131,200\u00a0ms; downstream processing adds "
        "200\u2013500\u00a0ms \u2014 well within the 3\u00a0s target."
    )
    p.sp(2)

    # Risk register (column-width)
    p.tbl(
        ["Risk","Impact","Prob.","Mitigation"],
        [
            ["Deepgram downtime","High","Med",
             "Circuit-breaker auto-routes to AWS Transcribe."],
            ["LLM hallucination","Med","Med",
             "Deterministic engine is independent; agent = one weighted input."],
            ["Deepfake evasion","Med","Low",
             "Evading spectral check \u2260 bypassing keyword engine."],
            ["PII data breach","High","Low",
             "PII masked before write; audio not persisted across sessions."],
            ["Scaling bottleneck","Med","Med",
             "Semaphore (max 50 STT) + stateless horizontal scaling."],
            ["New scam vocab","Med","High",
             "Hot-reload keyword lists; Gemini agent adapts semantically."],
        ],
        [COL_W*0.22, COL_W*0.10, COL_W*0.10, COL_W*0.58],
        "Table II \u2014 Risk register.",
        sz=SZ_XS
    )
    p.sp(3)

    # ══════════════════════════════════════════════════════════
    # IX. CONCLUSION
    # ══════════════════════════════════════════════════════════
    p.sec("CONCLUSION")
    p.tx(
        "FraudGuard AI demonstrates that real-time, in-call fraud protection is technically "
        "and economically viable today. The decisive design choice \u2014 using an autonomous "
        "agent that calls tools before deciding, rather than applying a single classifier \u2014 "
        "produces more reliable verdicts and leaves a full audit trail. The system is "
        "production-ready, horizontally scalable, and privacy-preserving by design. Immediate "
        "next steps: public beta with Vietnamese telecom partners, fine-tuned deepfake detection "
        "model, and expansion to additional Southeast Asian languages."
    )
    p.sp(4)

    # ── REFERENCES ────────────────────────────────────────────
    p.need(10)
    p.set_font("F","B",SZ_SECT); p.set_xy(p.cx,p._y)
    p.cell(p.cw,6,"REFERENCES",align="L"); p._y+=7

    refs=[
        "[1] National Cybersecurity Center (NCSC), \u201cVietnam Cybersecurity Report 2023,\u201d "
        "Ministry of Information and Communications, Hanoi, 2023.",
        "[2] Y. Wei, Y. Wang, and S. Gao, \u201cA Survey on Telephone Fraud Detection Using Machine "
        "Learning,\u201d IEEE Access, vol.\u00a011, pp.\u00a045231\u201345248, 2023.",
        "[3] S. Yao et al., \u201cReAct: Synergizing Reasoning and Acting in Language Models,\u201d "
        "Proc. ICLR 2023, Kigali, Rwanda, 2023.",
        "[4] Deepgram Inc., \u201cNova-2 Streaming API,\u201d Deepgram Documentation, 2024. "
        "[Online]. Available: https://developers.deepgram.com.",
        "[5] Google DeepMind, \u201cGemini: A Family of Highly Capable Multimodal Models,\u201d "
        "arXiv:2312.11805, Dec. 2023.",
        "[6] M. Todisco et al., \u201cASVspoof 2019: Future Horizons in Spoofed and Fake Audio "
        "Detection,\u201d Proc. Interspeech 2019, pp.\u00a01008\u20131012.",
        "[7] Amazon Web Services, \u201cAmazon Transcribe Developer Guide,\u201d 2024. "
        "[Online]. Available: https://docs.aws.amazon.com/transcribe.",
        "[8] M. T. Nygard, Release It! Design and Deploy Production-Ready Software, 2nd\u00a0ed. "
        "Pragmatic Bookshelf, 2018.",
        "[9] R. C. Martin, Clean Architecture: A Craftsman\u2019s Guide to Software Structure "
        "and Design. Prentice Hall, 2017.",
    ]
    for ref in refs:
        p.need(8)
        p.tx(ref, sz=SZ_REF, lh=SZ_REF*0.46)
        p.sp(1)

    # Close the final separator segment on the last page
    if p._twocol:
        p._draw_sep_seg(p._sep_seg_start, BOTTOM)

    p.output(out)
    print(f"Done! Pages: {p.page}  ->  {out}")


if __name__ == "__main__":
    out = r"C:\Users\trinh\Downloads\FraudGuard_AI_Proposal_v5_IEEE.pdf"
    build(out)
