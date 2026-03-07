var fe = Object.defineProperty;
var ye = (o, t, n) => t in o ? fe(o, t, { enumerable: !0, configurable: !0, writable: !0, value: n }) : o[t] = n;
var g = (o, t, n) => ye(o, typeof t != "symbol" ? t + "" : t, n);
import { Ruler as jt, X as be, Undo2 as ht, Redo2 as gt, Bold as pt, Italic as ut, Underline as mt, Strikethrough as Gt, AlignLeft as ft, AlignCenter as yt, AlignRight as bt, AlignJustify as Ut, List as vt, ListOrdered as wt, Table as Yt, Link as Xt, Image as xt, Download as qt, Upload as Zt, Grid2x2 as Vt, PaintBucket as Kt, Eraser as ve, Scissors as Jt, RectangleVertical as Qt, RectangleHorizontal as te, Columns2 as we, Grid3x3 as ee, Pilcrow as ne, BookOpen as xe, BarChart2 as Ce, Code2 as Se, ChevronLeft as Ee, ChevronRight as Ht } from "lucide";
class ke {
  constructor() {
    g(this, "dotNetRef", null);
    g(this, "readyPromise");
    g(this, "resolveReady");
    var n;
    this.readyPromise = new Promise((e) => {
      this.resolveReady = e;
    });
    const t = (n = window.getDotNetReference) == null ? void 0 : n.call(window);
    t && (this.dotNetRef = t, this.resolveReady()), window.addEventListener("engine-ready", ((e) => {
      this.dotNetRef = e.detail, this.resolveReady();
    }));
  }
  /** Wait for the WASM runtime to load and the .NET reference to be available */
  async waitForReady() {
    return this.readyPromise;
  }
  get isReady() {
    return this.dotNetRef !== null;
  }
  // ─── Lifecycle ─────────────────────────────────────────────
  async initialize(t) {
    return this.invoke("Initialize", t ?? null);
  }
  // ─── Text Editing ──────────────────────────────────────────
  async insertText(t, n) {
    return this.invoke("InsertText", t, I(n));
  }
  async deleteBackward(t) {
    return this.invoke("DeleteBackward", I(t));
  }
  async deleteForward(t) {
    return this.invoke("DeleteForward", I(t));
  }
  async splitParagraph(t) {
    return this.invoke("SplitParagraph", I(t));
  }
  async insertBreak(t, n) {
    return this.invoke("InsertBreak", t, I(n));
  }
  async deleteSelection(t) {
    return this.invoke("DeleteSelection", I(t));
  }
  async pasteText(t, n) {
    return this.invoke("PasteText", t, I(n));
  }
  // ─── Formatting ────────────────────────────────────────────
  async toggleFormat(t, n) {
    return this.invoke("ToggleFormat", t, I(n));
  }
  async setParagraphStyle(t, n) {
    return this.invoke("SetParagraphStyle", t, I(n));
  }
  async setAlignment(t, n) {
    return this.invoke("SetAlignment", t, I(n));
  }
  async toggleList(t, n) {
    return this.invoke("ToggleList", t, I(n));
  }
  async setIndent(t, n, e) {
    return this.invoke(
      "SetIndent",
      t,
      n,
      I(e)
    );
  }
  // ─── Insertions ────────────────────────────────────────────
  async insertTable(t, n, e) {
    return this.invoke("InsertTable", t, n, I(e));
  }
  async setTableCellBorders(t, n, e) {
    return this.invoke(
      "SetTableCellBorders",
      t,
      n ? JSON.stringify(n) : null,
      I(e)
    );
  }
  async setTableCellShading(t, n, e) {
    return this.invoke("SetTableCellShading", t, n, I(e));
  }
  async insertHyperlink(t, n, e) {
    return this.invoke("InsertHyperlink", t, n, I(e));
  }
  async insertImage(t, n) {
    return this.invoke("InsertImage", JSON.stringify(t), I(n));
  }
  async setImageSize(t, n, e) {
    return this.invoke("SetImageSize", t, n, e);
  }
  async setImageRotation(t, n) {
    return this.invoke("SetImageRotation", t, n);
  }
  async setImageWrapMode(t, n) {
    return this.invoke("SetImageWrapMode", t, n);
  }
  async setImagePosition(t, n, e) {
    return this.invoke("SetImagePosition", t, n, e);
  }
  async deleteImageRun(t) {
    return this.invoke("DeleteImageRun", t);
  }
  // ─── Sections ─────────────────────────────────────────
  async insertSectionBreak(t, n) {
    const e = {
      nextPage: "NextPage",
      continuous: "Continuous",
      evenPage: "EvenPage",
      oddPage: "OddPage"
    };
    return this.invoke(
      "InsertSectionBreak",
      e[t] ?? "NextPage",
      I(n)
    );
  }
  async removeSectionBreak(t) {
    return this.invoke("RemoveSectionBreak", I(t));
  }
  async setPageOrientation(t, n) {
    const e = {
      portrait: "Portrait",
      landscape: "Landscape"
    };
    return this.invoke(
      "SetPageOrientation",
      e[t] ?? "Portrait",
      I(n)
    );
  }
  async setColumns(t, n, e) {
    return this.invoke(
      "SetColumns",
      t,
      n,
      I(e)
    );
  }
  // ─── History ───────────────────────────────────────────────
  async undo() {
    return this.invoke("Undo");
  }
  async redo() {
    return this.invoke("Redo");
  }
  // ─── File I/O ──────────────────────────────────────────────
  async exportDocx() {
    return await this.requireRef().invokeMethodAsync("ExportDocx");
  }
  async importDocx(t) {
    const e = await this.requireRef().invokeMethodAsync("ImportDocx", t);
    return JSON.parse(e);
  }
  async setFontFamily(t, n) {
    return this.invoke("SetFontFamily", t, I(n));
  }
  async setFontSize(t, n) {
    return this.invoke("SetFontSize", t, I(n));
  }
  // ─── Query ─────────────────────────────────────────────────
  async getFormatState(t) {
    const e = await this.requireRef().invokeMethodAsync("GetFormatState", I(t));
    return JSON.parse(e);
  }
  // ─── Internal ──────────────────────────────────────────────
  async invoke(t, ...n) {
    const i = await this.requireRef().invokeMethodAsync(t, ...n);
    return JSON.parse(i);
  }
  requireRef() {
    if (!this.dotNetRef)
      throw new Error(
        "Engine not ready. Call waitForReady() before invoking methods."
      );
    return this.dotNetRef;
  }
}
function I(o) {
  return JSON.stringify(o);
}
const Te = /* @__PURE__ */ new Set([
  "arimo",
  "carlito",
  "tinos",
  "cousine",
  "caladea",
  "roboto",
  "open sans",
  "lato",
  "merriweather",
  "noto sans",
  "source sans pro",
  "ubuntu",
  "oswald",
  "pt sans",
  "pt serif",
  "raleway",
  "nunito"
]), Nt = /* @__PURE__ */ new Set();
function ie(o) {
  for (const t of o) {
    const n = t.toLowerCase().trim();
    !Nt.has(n) && Te.has(n) && (Nt.add(n), Le(t.trim()));
  }
}
function Le(o) {
  if (!document.querySelector("link[data-gf-preconnect]")) {
    const e = Object.assign(document.createElement("link"), {
      rel: "preconnect",
      href: "https://fonts.googleapis.com"
    });
    e.setAttribute("data-gf-preconnect", "");
    const i = Object.assign(document.createElement("link"), {
      rel: "preconnect",
      href: "https://fonts.gstatic.com"
    });
    i.crossOrigin = "anonymous", i.setAttribute("data-gf-preconnect", ""), document.head.append(e, i);
  }
  const t = encodeURIComponent(o), n = document.createElement("link");
  n.rel = "stylesheet", n.href = `https://fonts.googleapis.com/css2?family=${t}:ital,wght@0,400;0,700;1,400;1,700&display=swap`, document.head.appendChild(n);
}
const Ct = /* @__PURE__ */ new Map();
function Pe(o, t) {
  oe(o, t), ie(Ie(o));
}
function Ie(o) {
  const t = /* @__PURE__ */ new Set();
  function n(e) {
    var i;
    for (const s of e) {
      const a = (i = s.styles) == null ? void 0 : i["font-family"];
      if (a) {
        const r = a.match(/['"]?([^'",]+)['"]?/);
        r && t.add(r[1].trim());
      }
      s.children && n(s.children);
    }
  }
  return n(o), t;
}
function oe(o, t) {
  var i;
  const n = new Set(o.map((s) => s.id));
  for (const s of Array.from(t.children)) {
    const a = s, r = a.dataset.nodeId;
    r && !n.has(r) && (rt(a), t.removeChild(a));
  }
  let e = t.firstChild;
  for (const s of o) {
    const a = Ct.get(s.id);
    if (a && a.tagName.toLowerCase() === s.tag)
      Be(a, s), a !== e ? t.insertBefore(a, e) : e = a.nextSibling;
    else {
      a && (a === e && (e = a.nextSibling), rt(a), (i = a.parentNode) == null || i.removeChild(a));
      const r = St(s);
      se(r), t.insertBefore(r, e);
    }
  }
}
function Be(o, t) {
  const n = t.styles ? Object.entries(t.styles).map(([e, i]) => `${e}:${i}`).join(";") : "";
  if (o.style.cssText !== n && (o.style.cssText = n), Re(o, t.attrs ?? {}), t.text !== void 0) {
    const e = t.text || "​";
    o.textContent !== e && (o.textContent = e);
  }
  t.children && oe(t.children, o);
}
function Re(o, t) {
  for (const [n, e] of Object.entries(t))
    if (n.startsWith("data-")) {
      const i = lt(n.slice(5));
      o.dataset[i] !== e && (o.dataset[i] = e);
    } else
      o.getAttribute(n) !== e && o.setAttribute(n, e);
  for (const n of Array.from(o.attributes)) {
    const e = n.name;
    e === "data-node-id" || e === "style" || (e.startsWith("data-") ? e in t || delete o.dataset[lt(e.slice(5))] : e in t || o.removeAttribute(e));
  }
}
function se(o) {
  o.dataset.nodeId && Ct.set(o.dataset.nodeId, o);
  for (const t of Array.from(o.children))
    se(t);
}
function rt(o) {
  o.dataset.nodeId && Ct.delete(o.dataset.nodeId);
  for (const t of Array.from(o.children))
    rt(t);
}
function St(o) {
  const t = document.createElement(o.tag);
  if (t.dataset.nodeId = o.id, o.styles && (t.style.cssText = Object.entries(o.styles).map(([n, e]) => `${n}:${e}`).join(";")), o.attrs)
    for (const [n, e] of Object.entries(o.attrs))
      n.startsWith("data-") ? t.dataset[lt(n.slice(5))] = e : t.setAttribute(n, e);
  if (o.text !== void 0 && o.text !== null && (t.textContent = o.text || "​"), o.children)
    for (const n of o.children)
      t.appendChild(St(n));
  return t;
}
function Ae(o) {
  const t = St(o);
  return t.contentEditable = "false", t.style.userSelect = "none", t;
}
function lt(o) {
  return o.replace(/-([a-z])/g, (t, n) => n.toUpperCase());
}
function Mt(o, t, n) {
  var d;
  let e = o.nodeType === Node.TEXT_NODE ? o.parentElement : o;
  for (; e && e !== n && !((d = e.dataset) != null && d.nodeId); )
    e = e.parentElement;
  if (!e || e === n) return null;
  const i = e;
  let s = i.parentElement;
  for (; s && s !== n; ) {
    const h = s.tagName.toLowerCase();
    if (h === "p" || h.match(/^h[1-6]$/)) break;
    s = s.parentElement;
  }
  if (!s || s === n) return null;
  let a = t;
  o.nodeType === Node.TEXT_NODE && o.textContent === "​" && (a = 0);
  const r = Ne(i, s), l = Fe(s, n);
  if (l) {
    const { tableEl: h, rowIndex: u, cellIndex: p, cellBlockIndex: f } = l, b = Dt(h, n);
    return b < 0 ? null : { blockIndex: b, inlineIndex: r, offset: a, cell: { rowIndex: u, cellIndex: p, cellBlockIndex: f } };
  }
  const c = Dt(s, n);
  return c < 0 ? null : { blockIndex: c, inlineIndex: r, offset: a };
}
function T(o) {
  const t = window.getSelection();
  if (!t || t.rangeCount === 0) return null;
  const n = t.anchorNode ? Mt(t.anchorNode, t.anchorOffset, o) : null, e = t.focusNode ? Mt(t.focusNode, t.focusOffset, o) : null;
  return n ? {
    anchor: n,
    focus: e ?? n
  } : null;
}
function He(o, t) {
  const n = Ft(o.anchor, t);
  if (!n) return;
  const e = window.getSelection();
  if (e)
    if (o.isCollapsed)
      e.setBaseAndExtent(
        n.node,
        n.offset,
        n.node,
        n.offset
      );
    else {
      const i = Ft(o.focus, t);
      if (!i) return;
      e.setBaseAndExtent(
        n.node,
        n.offset,
        i.node,
        i.offset
      );
    }
}
function Ft(o, t) {
  var d;
  const n = ae(t);
  if (o.blockIndex >= n.length) return null;
  const e = n[o.blockIndex];
  let i;
  if (o.cell) {
    if (e.tagName.toLowerCase() !== "table") return null;
    const h = ce(e);
    if (o.cell.rowIndex >= h.length) return null;
    const u = h[o.cell.rowIndex], p = Array.from(u.children);
    if (o.cell.cellIndex >= p.length) return null;
    const f = p[o.cell.cellIndex], b = Array.from(f.children);
    if (o.cell.cellBlockIndex >= b.length) return null;
    i = b[o.cell.cellBlockIndex];
  } else
    i = e;
  const s = re(i);
  if (o.inlineIndex >= s.length) return null;
  const a = s[o.inlineIndex], r = le(a);
  if (!r)
    return { node: a, offset: 0 };
  const l = r.textContent === "​" ? 0 : ((d = r.textContent) == null ? void 0 : d.length) ?? 0, c = Math.min(o.offset, l);
  return r.textContent === "​" ? { node: r, offset: 1 } : { node: r, offset: c };
}
function ae(o) {
  var n, e;
  const t = [];
  for (const i of o.children) {
    const s = i, a = (n = s.tagName) == null ? void 0 : n.toLowerCase();
    if (a === "section")
      for (const r of s.children) {
        const l = (e = r.tagName) == null ? void 0 : e.toLowerCase();
        (l === "p" || l != null && l.match(/^h[1-6]$/) || l === "table") && t.push(r);
      }
    else (a === "p" || a != null && a.match(/^h[1-6]$/) || a === "table") && t.push(s);
  }
  return t;
}
function re(o) {
  var n;
  const t = [];
  for (const e of o.children) {
    const i = e;
    (n = i.dataset) != null && n.nodeId && t.push(i);
  }
  return t;
}
function le(o) {
  for (const t of o.childNodes) {
    if (t.nodeType === Node.TEXT_NODE) return t;
    if (t.nodeType === Node.ELEMENT_NODE) {
      const n = le(t);
      if (n) return n;
    }
  }
  return null;
}
function Dt(o, t) {
  var i;
  let n = o;
  for (; n.parentElement && n.parentElement !== t && !(((i = n.parentElement.tagName) == null ? void 0 : i.toLowerCase()) === "section" && n.parentElement.parentElement === t); )
    n = n.parentElement;
  return ae(t).indexOf(n);
}
function Ne(o, t) {
  const n = re(t);
  for (let e = 0; e < n.length; e++)
    if (n[e] === o || n[e].contains(o))
      return e;
  return 0;
}
function Me(o, t) {
  let n = o;
  for (; n && n !== t; ) {
    if (n.tagName.toLowerCase() === "table") return n;
    n = n.parentElement;
  }
  return null;
}
function ce(o) {
  const t = [];
  for (const n of o.children) {
    const e = n.tagName.toLowerCase();
    if (e === "tr")
      t.push(n);
    else if (e === "thead" || e === "tbody" || e === "tfoot")
      for (const i of n.children)
        i.tagName.toLowerCase() === "tr" && t.push(i);
  }
  return t;
}
function Fe(o, t) {
  let n = o.parentElement;
  for (; n && n !== t; ) {
    if (n.tagName.toLowerCase() === "td" || n.tagName.toLowerCase() === "th") {
      const e = n, i = e.parentElement, s = Me(i, t);
      if (!s) return null;
      const r = ce(s).indexOf(i);
      if (r < 0) return null;
      const l = Array.from(i.children).indexOf(e);
      if (l < 0) return null;
      let c = o;
      for (; c.parentElement !== e; ) {
        if (!c.parentElement || c.parentElement === t) return null;
        c = c.parentElement;
      }
      const d = Array.from(e.children).indexOf(c);
      return d < 0 ? null : { tableEl: s, rowIndex: r, cellIndex: l, cellBlockIndex: d };
    }
    n = n.parentElement;
  }
  return null;
}
const De = 816, $e = 1056, Oe = 96, We = 96, ze = 96, _e = 96, $ = 24;
function H(o) {
  return Math.round(o * 96 / 1440);
}
class je {
  constructor(t, n = {}) {
    g(this, "container");
    g(this, "pagesWrapper");
    g(this, "pageFrame");
    g(this, "canvas");
    g(this, "overlay");
    g(this, "config");
    g(this, "sectionConfigs", []);
    g(this, "rawSections", []);
    g(this, "adjusting", !1);
    g(this, "breakBottomYPositions", []);
    g(this, "breakPageBottomYPositions", []);
    g(this, "pageSectionMap", []);
    /** Total number of pages after the last pagination. */
    g(this, "pageCount", 1);
    this.container = t, this.config = {
      pageWidth: n.pageWidth ?? De,
      pageHeight: n.pageHeight ?? $e,
      marginTop: n.marginTop ?? Oe,
      marginBottom: n.marginBottom ?? We,
      marginLeft: n.marginLeft ?? ze,
      marginRight: n.marginRight ?? _e
    }, this.pagesWrapper = document.createElement("div"), this.pagesWrapper.className = "pages-wrapper", this.pageFrame = document.createElement("div"), this.pageFrame.className = "page-frame", this.pageFrame.style.width = `${this.config.pageWidth}px`, this.pageFrame.style.minHeight = `${this.config.pageHeight}px`, this.canvas = document.createElement("div"), this.canvas.className = "editor-canvas", this.canvas.contentEditable = "true", this.canvas.spellcheck = !1, this.canvas.setAttribute("role", "textbox"), this.canvas.setAttribute("aria-multiline", "true"), this.canvas.style.width = `${this.config.pageWidth}px`, this.canvas.style.minHeight = `${this.config.pageHeight}px`, this.canvas.style.outline = "none", this.canvas.style.padding = "0", this.overlay = document.createElement("div"), this.overlay.className = "page-breaks-overlay", this.pageFrame.appendChild(this.canvas), this.pageFrame.appendChild(this.overlay), this.pagesWrapper.appendChild(this.pageFrame), this.container.appendChild(this.pagesWrapper);
  }
  get contentWidth() {
    return this.config.pageWidth - this.config.marginLeft - this.config.marginRight;
  }
  get contentHeight() {
    return this.config.pageHeight - this.config.marginTop - this.config.marginBottom;
  }
  getCanvas() {
    return this.canvas;
  }
  getDebugSectionData() {
    return this.rawSections.map((t, n) => {
      const e = this.sectionConfigs[n] ?? this.sectionConfigs[0];
      return {
        index: n,
        rawPageWidth: t.pageWidth,
        rawPageHeight: t.pageHeight,
        rawMarginTop: t.marginTop,
        rawMarginBottom: t.marginBottom,
        rawMarginLeft: t.marginLeft,
        rawMarginRight: t.marginRight,
        rawHeaderDistance: t.headerDistance ?? 720,
        rawFooterDistance: t.footerDistance ?? 720,
        pxPageWidth: e.pageWidth,
        pxPageHeight: e.pageHeight,
        pxMarginTop: e.marginTop,
        pxMarginBottom: e.marginBottom,
        pxMarginLeft: e.marginLeft,
        pxMarginRight: e.marginRight,
        pxHeaderDistance: e.headerDistance,
        pxFooterDistance: e.footerDistance,
        pxContentWidth: e.contentWidth,
        pxContentHeight: e.contentHeight
      };
    });
  }
  /**
   * Update page dimensions from sections metadata.
   * Frame becomes a transparent container at the max page width.
   * Sections handle their own paper styling and margins via inline padding.
   */
  updateFromSections(t) {
    if (t.length === 0) return;
    this.rawSections = t, this.sectionConfigs = t.map((i) => {
      const s = H(i.pageWidth), a = H(i.pageHeight), r = H(i.marginTop), l = H(i.marginBottom), c = H(i.marginLeft), d = H(i.marginRight);
      return {
        pageWidth: s,
        pageHeight: a,
        marginTop: r,
        marginBottom: l,
        marginLeft: c,
        marginRight: d,
        contentWidth: s - c - d,
        contentHeight: a - r - l,
        breakType: i.breakType,
        headers: i.headers,
        footers: i.footers,
        titlePage: i.titlePage ?? !1,
        headerDistance: H(i.headerDistance ?? 720),
        footerDistance: H(i.footerDistance ?? 720),
        columnCount: i.columnCount ?? 1
      };
    });
    const n = Math.max(...this.sectionConfigs.map((i) => i.pageWidth)), e = this.sectionConfigs[0];
    this.config.pageWidth = n, this.config.pageHeight = e.pageHeight, this.config.marginTop = e.marginTop, this.config.marginBottom = e.marginBottom, this.config.marginLeft = e.marginLeft, this.config.marginRight = e.marginRight, this.canvas.style.width = `${n}px`, this.canvas.style.minHeight = `${e.pageHeight}px`, this.pageFrame.style.width = `${n}px`, this.pageFrame.style.minHeight = `${e.pageHeight}px`, this.pageFrame.style.padding = "0";
  }
  /**
   * Recalculate pagination after every render.
   * Section-aware: handles different page heights per section,
   * forces page breaks at nextPage/evenPage/oddPage section boundaries,
   * and allows continuous flow for continuous breaks with matching dimensions.
   */
  updatePagination() {
    this.clearGapMargins(), this.sectionConfigs.length > 1 ? this.updatePaginationMultiSection() : this.updatePaginationSingleSection();
  }
  /** Single-section pagination logic with section padding awareness. */
  updatePaginationSingleSection() {
    const t = this.sectionConfigs[0], n = (t == null ? void 0 : t.contentHeight) ?? this.contentHeight, e = (t == null ? void 0 : t.marginBottom) ?? this.config.marginBottom, i = (t == null ? void 0 : t.marginTop) ?? this.config.marginTop, s = (t == null ? void 0 : t.pageWidth) ?? this.config.pageWidth, a = this.canvas.querySelector("section"), r = a ? a.offsetTop + a.clientTop + ((t == null ? void 0 : t.marginTop) ?? 0) : 0, l = a ? a.scrollHeight - ((t == null ? void 0 : t.marginTop) ?? 0) - ((t == null ? void 0 : t.marginBottom) ?? 0) : this.canvas.scrollHeight;
    let c = Math.max(1, Math.ceil(l / n));
    if (c <= 1) {
      this.pageCount = 1, this.breakBottomYPositions = [], this.breakPageBottomYPositions = [], this.pageSectionMap = [0];
      const d = (t == null ? void 0 : t.pageHeight) ?? this.config.pageHeight;
      a && (!t || t.columnCount <= 1) && (a.style.minHeight = `${d}px`), this.renderOverlays([], d), this.updateFrameHeight(d);
      return;
    }
    for (let d = 0; d < 3; d++) {
      this.clearGapMargins();
      const h = this.getAllBlockChildren();
      let u = 0;
      const p = [];
      for (let k = 1; k < c; k++) {
        const m = r + n * k + u, x = e + $ + i;
        let C = null, v = !1, L = !1;
        for (const E of h)
          if (E.offsetTop + E.offsetHeight > m) {
            if (E.tagName === "TABLE" && this.tryTableRowSplit(E, m, x, n) !== null) {
              p.push({
                y: m,
                marginBottom: e,
                marginTop: i,
                pageWidth: s,
                endingSectionIndex: 0,
                endingPageInSection: k - 1,
                startingSectionIndex: 0,
                startingPageInSection: k
              }), u += x, v = !0;
              break;
            }
            if ((E.tagName === "P" || /^H[1-6]$/.test(E.tagName)) && this.tryParaSplit(E, m, x, n) !== !1) {
              p.push({
                y: m,
                marginBottom: e,
                marginTop: i,
                pageWidth: s,
                endingSectionIndex: 0,
                endingPageInSection: k - 1,
                startingSectionIndex: 0,
                startingPageInSection: k
              }), u += x, L = !0;
              break;
            }
            if (E.offsetHeight > n) continue;
            C = E;
            break;
          }
        if (!v && !L && C) {
          p.push({
            y: m,
            marginBottom: e,
            marginTop: i,
            pageWidth: s,
            endingSectionIndex: 0,
            endingPageInSection: k - 1,
            startingSectionIndex: 0,
            startingPageInSection: k
          }), C.dataset.originalMarginTop || (C.dataset.originalMarginTop = C.style.marginTop || "");
          const E = parseFloat(C.style.marginTop) || 0, B = m - C.offsetTop + x;
          C.style.marginTop = `${E + B}px`;
          const N = m + x, A = C.offsetTop;
          A < N && (C.style.marginTop = `${E + B + (N - A)}px`), C.dataset.pageGap = "true", u += x;
        }
      }
      const b = (a ? a.scrollHeight - ((t == null ? void 0 : t.marginTop) ?? 0) - ((t == null ? void 0 : t.marginBottom) ?? 0) : this.canvas.scrollHeight) - u, w = Math.max(1, Math.ceil(b / n));
      if (w === c) {
        this.pageCount = c, this.breakBottomYPositions = p.map((x) => x.y + x.marginBottom + $ + x.marginTop), this.breakPageBottomYPositions = p.map((x) => x.y + x.marginBottom), this.pageSectionMap = Array(this.pageCount).fill(0);
        const m = ((t == null ? void 0 : t.pageHeight) ?? this.config.pageHeight) * c + $ * (c - 1);
        a && (!t || t.columnCount <= 1) && (a.style.minHeight = `${m}px`), this.renderOverlays(p, m), this.updateFrameHeight(m);
        return;
      }
      c = w;
    }
    this.renderCurrentState();
  }
  /**
   * Multi-section pagination.
   * Walks through section elements in DOM, applying per-section page heights.
   * Forces page breaks at section boundaries based on break type.
   */
  updatePaginationMultiSection() {
    var e, i;
    const t = Array.from(this.canvas.children).filter(
      (s) => {
        var a;
        return ((a = s.tagName) == null ? void 0 : a.toLowerCase()) === "section";
      }
    );
    if (t.length === 0) {
      this.updatePaginationSingleSection();
      return;
    }
    let n = -1;
    for (let s = 0; s < 3; s++) {
      this.clearGapMargins();
      const a = [];
      let r = 0, l = 0;
      for (let c = 0; c < t.length; c++) {
        const d = t[c], h = this.sectionConfigs[c] ?? this.sectionConfigs[0], u = c > 0 ? this.sectionConfigs[c - 1] ?? this.sectionConfigs[0] : h, p = d.clientTop + h.marginTop, f = d.offsetTop + p;
        let b = 0, w = 0;
        if (c > 0) {
          const m = h.pageWidth === u.pageWidth && h.pageHeight === u.pageHeight;
          if (!(u.breakType === "continuous" && m)) {
            const C = u.marginBottom + $ + h.marginTop, v = this.getFirstBlockChild(d);
            if (v) {
              const L = d.offsetTop - u.marginBottom;
              a.push({
                y: L,
                marginBottom: u.marginBottom,
                marginTop: h.marginTop,
                pageWidth: Math.max(u.pageWidth, h.pageWidth),
                endingSectionIndex: c - 1,
                endingPageInSection: l,
                startingSectionIndex: c,
                startingPageInSection: 0
              });
              let E = v;
              if (v.dataset.sectionBreak != null && ((e = v.textContent) == null ? void 0 : e.trim()) === "") {
                const A = this.getBlockChildrenOf(t[c - 1]), j = A[A.length - 1] ?? null;
                j && !j.dataset.sectionBreak && (j.dataset.sectionBreak = u.breakType ?? "nextpage", j.dataset.sbInjected = "true"), v.style.display = "none", v.dataset.sbHolderHidden = "true", E = this.getBlockChildrenOf(d).find((X) => X !== v) ?? v;
              }
              E.dataset.originalMarginTop || (E.dataset.originalMarginTop = E.style.marginTop || "");
              const B = parseFloat(E.style.marginTop) || 0, N = L + C - E.offsetTop;
              N > 0 && (E.style.marginTop = `${B + N}px`, E.dataset.pageGap = "true");
            }
            r = 0;
          }
        }
        const k = this.getBlockChildrenOf(d);
        for (const m of k) {
          if (m.dataset.pageGap) continue;
          const x = m.offsetTop + m.offsetHeight, C = f + (h.contentHeight - r) + h.contentHeight * b + w;
          if (x > C) {
            const v = h.marginBottom + $ + h.marginTop;
            if (m.dataset.sectionBreak != null && ((i = m.textContent) == null ? void 0 : i.trim()) === "")
              continue;
            if (m.tagName === "TABLE" && this.tryTableRowSplit(m, C, v, h.contentHeight) !== null) {
              a.push({
                y: C,
                marginBottom: h.marginBottom,
                marginTop: h.marginTop,
                pageWidth: h.pageWidth,
                endingSectionIndex: c,
                endingPageInSection: b,
                startingSectionIndex: c,
                startingPageInSection: b + 1
              }), b++, w += v, r = 0;
              continue;
            }
            if ((m.tagName === "P" || /^H[1-6]$/.test(m.tagName)) && this.tryParaSplit(m, C, v, h.contentHeight) !== !1) {
              a.push({
                y: C,
                marginBottom: h.marginBottom,
                marginTop: h.marginTop,
                pageWidth: h.pageWidth,
                endingSectionIndex: c,
                endingPageInSection: b,
                startingSectionIndex: c,
                startingPageInSection: b + 1
              }), b++, w += v, r = 0;
              continue;
            }
            if (m.offsetHeight <= h.contentHeight) {
              a.push({
                y: C,
                marginBottom: h.marginBottom,
                marginTop: h.marginTop,
                pageWidth: h.pageWidth,
                endingSectionIndex: c,
                endingPageInSection: b,
                startingSectionIndex: c,
                startingPageInSection: b + 1
              }), m.dataset.originalMarginTop || (m.dataset.originalMarginTop = m.style.marginTop || "");
              const E = parseFloat(m.style.marginTop) || 0, B = C - m.offsetTop + v;
              m.style.marginTop = `${E + B}px`;
              const N = C + v, A = m.offsetTop;
              A < N && (m.style.marginTop = `${E + B + (N - A)}px`), m.dataset.pageGap = "true", b++, w += v, r = 0;
            }
          }
        }
        if (k.length > 0) {
          const m = k[k.length - 1];
          r = (m.offsetTop + m.offsetHeight - f - w) % h.contentHeight || h.contentHeight;
        }
        l = b;
      }
      if (a.length === n || a.length === 0) {
        this.pageCount = a.length + 1;
        const c = t.map((p) => p.offsetTop);
        this.applySectionMinHeights(t, a);
        const d = t.map((p) => p.offsetTop);
        for (const p of a)
          if (p.endingSectionIndex !== p.startingSectionIndex) {
            const f = this.sectionConfigs[p.endingSectionIndex];
            p.y = t[p.startingSectionIndex].offsetTop - f.marginBottom;
          } else {
            const f = p.startingSectionIndex;
            p.y += d[f] - c[f];
          }
        this.breakBottomYPositions = a.map((p) => p.y + p.marginBottom + $ + p.marginTop), this.breakPageBottomYPositions = a.map((p) => p.y + p.marginBottom), this.pageSectionMap = [0, ...a.map((p) => p.startingSectionIndex)];
        const h = t[t.length - 1], u = h.offsetTop + h.offsetHeight;
        this.renderOverlays(a, u), this.updateFrameHeight(this.canvas.scrollHeight);
        return;
      }
      n = a.length;
    }
    this.renderCurrentState();
  }
  /** Get all block-level children across all sections (or direct canvas children). */
  getAllBlockChildren() {
    var n;
    const t = [];
    for (const e of this.canvas.children) {
      const i = e, s = (n = i.tagName) == null ? void 0 : n.toLowerCase();
      s === "section" ? t.push(...this.getBlockChildrenOf(i)) : (s === "p" || s != null && s.match(/^h[1-6]$/) || s === "table") && t.push(i);
    }
    return t;
  }
  /** Get block children of a section element. */
  getBlockChildrenOf(t) {
    var e;
    const n = [];
    for (const i of t.children) {
      const s = (e = i.tagName) == null ? void 0 : e.toLowerCase();
      (s === "p" || s != null && s.match(/^h[1-6]$/) || s === "table") && n.push(i);
    }
    return n;
  }
  /** Get first block child of a section element. */
  getFirstBlockChild(t) {
    var n;
    for (const e of t.children) {
      const i = (n = e.tagName) == null ? void 0 : n.toLowerCase();
      if (i === "p" || i != null && i.match(/^h[1-6]$/) || i === "table")
        return e;
    }
    return null;
  }
  /**
   * Attempt to split a table at a row boundary at `boundaryY`.
   * Inserts a gap `<tr>` row before the first row that crosses the boundary.
   * Returns the gap row height if a split was performed; null if the caller
   * should fall back to treating the table as an atomic block.
   */
  tryTableRowSplit(t, n, e, i) {
    const s = Array.from(
      t.querySelectorAll(":scope > tbody > tr, :scope > tr")
    ), a = t.offsetTop;
    for (let r = 0; r < s.length; r++) {
      const l = s[r], c = a + l.offsetTop;
      if (c + l.offsetHeight > n) {
        if (r === 0 || l.offsetHeight > i)
          return null;
        const h = n - c + e, u = Math.max(
          1,
          ...Array.from(
            t.querySelectorAll(":scope > tbody > tr, :scope > tr")
          ).slice(0, r).map((b) => b.querySelectorAll("td, th").length)
        ), p = document.createElement("tr");
        p.dataset.pageGapRow = "true", p.contentEditable = "false", p.style.height = `${h}px`;
        const f = document.createElement("td");
        return f.setAttribute("colspan", String(u)), f.style.cssText = "padding:0;border:none;height:inherit", p.appendChild(f), l.parentElement.insertBefore(p, l), h;
      }
    }
    return null;
  }
  /**
   * Attempt to split a paragraph inline at `boundaryY` by inserting a
   * `<span data-para-page-gap>` gap span at the character boundary.
   * Returns the actual span height used if a gap span was inserted; false if
   * the caller should fall back to pushing the whole paragraph.
   */
  tryParaSplit(t, n, e, i) {
    if (t.dataset.keepLines === "true" || t.dataset.pageBreakBefore === "true" || t.offsetHeight === 0 || t.offsetHeight > i) return !1;
    const s = this.canvas.getBoundingClientRect(), a = s.top + n, r = document.createTreeWalker(
      t,
      NodeFilter.SHOW_TEXT,
      {
        acceptNode: (x) => {
          let C = x.parentNode;
          for (; C && C !== t; ) {
            if (C instanceof HTMLElement && C.dataset.paraPageGap === "true")
              return NodeFilter.FILTER_REJECT;
            C = C.parentNode;
          }
          return NodeFilter.FILTER_ACCEPT;
        }
      }
    ), l = document.createRange();
    let c = null, d = 0, h;
    for (; (h = r.nextNode()) !== null; ) {
      const x = h.length;
      if (x === 0) continue;
      l.setStart(h, 0), l.setEnd(h, x);
      const C = l.getBoundingClientRect();
      if (C.bottom <= a)
        continue;
      if (C.top >= a) {
        c = h, d = 0;
        break;
      }
      let v = 0, L = x;
      for (; v < L; ) {
        const E = v + L >> 1;
        l.setStart(h, 0), l.setEnd(h, E + 1), l.getBoundingClientRect().bottom <= a ? v = E + 1 : L = E;
      }
      if (d = v, d > 0 && d < x) {
        const E = h.data.charCodeAt(d - 1);
        E >= 55296 && E <= 56319 && d++;
      }
      c = h;
      break;
    }
    if (c === null) return !1;
    const u = c.length;
    l.setStart(c, d), l.setEnd(c, Math.min(d + 1, u));
    const f = l.getBoundingClientRect().top - s.top, b = Math.max(0, n - f + e);
    l.setStart(c, d), l.collapse(!0);
    const w = document.createElement("span");
    w.dataset.paraPageGap = "true", w.contentEditable = "false", w.setAttribute("aria-hidden", "true"), w.style.cssText = `display:block;height:${b}px;font-size:0;line-height:0;pointer-events:none;user-select:none;`, l.insertNode(w), w.offsetHeight;
    const k = document.createTreeWalker(t, NodeFilter.SHOW_TEXT);
    let m = null;
    for (; ; ) {
      const x = k.nextNode();
      if (!x) break;
      if (!(w.compareDocumentPosition(x) & Node.DOCUMENT_POSITION_FOLLOWING)) continue;
      let C = x.parentNode, v = !1;
      for (; C && C !== t; ) {
        if (C instanceof HTMLElement && C.dataset.paraPageGap === "true") {
          v = !0;
          break;
        }
        C = C.parentNode;
      }
      if (!v && x.length > 0) {
        m = x;
        break;
      }
    }
    if (m) {
      const x = document.createRange();
      x.setStart(m, 0), x.setEnd(m, Math.min(1, m.length));
      const v = x.getBoundingClientRect().top - s.top, E = n + e - v;
      if (Math.abs(E) > 0.5) {
        const B = Math.max(0, b + E);
        return w.style.height = `${B}px`, B;
      }
    }
    return b;
  }
  /** Remove gap margins from all children that had them injected. */
  clearGapMargins() {
    const t = this.canvas.querySelectorAll("[data-page-gap]");
    for (const r of t)
      r.style.marginTop = r.dataset.originalMarginTop || "", delete r.dataset.pageGap, delete r.dataset.originalMarginTop;
    const n = this.canvas.querySelectorAll("tr[data-page-gap-row]");
    for (const r of n)
      r.remove();
    const e = this.canvas.querySelectorAll("span[data-para-page-gap]");
    for (const r of e)
      r.remove();
    const i = this.canvas.querySelectorAll("[data-sb-holder-hidden]");
    for (const r of i)
      r.style.display = "", delete r.dataset.sbHolderHidden;
    const s = this.canvas.querySelectorAll("[data-sb-injected]");
    for (const r of s)
      delete r.dataset.sectionBreak, delete r.dataset.sbInjected;
    const a = this.canvas.querySelectorAll("section");
    for (let r = 0; r < a.length; r++) {
      const l = this.sectionConfigs[r] ?? this.sectionConfigs[0];
      l && l.columnCount <= 1 ? a[r].style.minHeight = `${l.pageHeight}px` : a[r].style.minHeight = "";
    }
  }
  /**
   * Set minHeight on each section element to ensure all pages render at full height.
   * Counts pages per section from break info (within-section breaks add a page).
   */
  applySectionMinHeights(t, n) {
    const e = new Array(t.length).fill(1);
    for (const i of n)
      i.endingSectionIndex === i.startingSectionIndex && e[i.startingSectionIndex]++;
    for (let i = 0; i < t.length; i++) {
      const s = this.sectionConfigs[i] ?? this.sectionConfigs[0];
      if (s.columnCount > 1) {
        t[i].style.minHeight = "";
        continue;
      }
      const a = e[i], r = s.pageHeight * a + $ * (a - 1);
      t[i].style.minHeight = `${r}px`;
    }
  }
  /** Remove all page-break overlay elements. */
  clearOverlays() {
    this.overlay.innerHTML = "";
  }
  /**
   * Resolve which header RenderNode[] to use for a given page.
   * Rules: first page of section + titlePage + "first" key → "first";
   *        even page number + "even" key → "even"; otherwise → "default".
   */
  resolveHeader(t, n) {
    const e = this.sectionConfigs[t];
    return e != null && e.headers ? n === 0 && e.titlePage && e.headers.first ? e.headers.first : (n + 1) % 2 === 0 && e.headers.even ? e.headers.even : e.headers.default ?? e.headers.first ?? null : null;
  }
  /** Same as resolveHeader but for footers. */
  resolveFooter(t, n) {
    const e = this.sectionConfigs[t];
    return e != null && e.footers ? n === 0 && e.titlePage && e.footers.first ? e.footers.first : (n + 1) % 2 === 0 && e.footers.even ? e.footers.even : e.footers.default ?? e.footers.first ?? null : null;
  }
  /**
   * Render header or footer content inside a margin zone element.
   * Creates a positioned div, renders RenderNode children as read-only DOM,
   * then substitutes dynamic field values (PAGE, NUMPAGES) with correct numbers.
   */
  renderHeaderFooterInZone(t, n, e, i, s, a) {
    const r = document.createElement("div");
    r.className = e === "header" ? "page-break-header-content" : "page-break-footer-content", r.style.width = `${i.contentWidth}px`, r.style.marginLeft = `${i.marginLeft}px`, r.style.marginRight = `${i.marginRight}px`, r.style.height = "100%", e === "header" ? r.style.paddingTop = `${i.headerDistance}px` : r.style.paddingTop = `${Math.max(0, i.marginBottom - i.footerDistance)}px`;
    for (const l of n)
      r.appendChild(Ae(l));
    for (const l of r.querySelectorAll('[data-field="PAGE"]'))
      l.textContent = String(s);
    for (const l of r.querySelectorAll('[data-field="NUMPAGES"]'))
      l.textContent = String(a);
    t.appendChild(r);
  }
  /** Render page-break strip overlays at computed positions. */
  renderOverlays(t, n) {
    this.clearOverlays();
    const e = t.length + 1;
    if (this.sectionConfigs.length > 0) {
      const i = this.sectionConfigs[0], s = this.resolveHeader(0, 0);
      if (s) {
        const a = document.createElement("div");
        a.className = "page-header-overlay", a.style.top = "0px", a.style.height = `${i.marginTop}px`, a.style.width = `${i.pageWidth}px`, a.style.left = "50%", a.style.transform = "translateX(-50%)", this.renderHeaderFooterInZone(a, s, "header", i, 1, e), this.overlay.appendChild(a);
      }
    }
    for (let i = 0; i < t.length; i++) {
      const s = t[i], a = s.marginBottom + $ + s.marginTop, r = s.y, l = document.createElement("div");
      l.className = "page-break-line", l.style.top = `${r}px`, l.style.height = `${a}px`, l.style.width = `${s.pageWidth}px`, l.style.left = "50%", l.style.transform = "translateX(-50%)";
      const c = this.sectionConfigs[s.endingSectionIndex], d = document.createElement("div");
      d.className = "page-break-margin-bottom", d.style.height = `${s.marginBottom}px`, c && (d.style.width = `${c.pageWidth}px`, d.style.margin = "0 auto");
      const h = this.resolveFooter(s.endingSectionIndex, s.endingPageInSection);
      h && c && this.renderHeaderFooterInZone(d, h, "footer", c, i + 1, e), l.appendChild(d);
      const u = document.createElement("div");
      u.className = "page-break-gap", u.style.height = `${$}px`, l.appendChild(u);
      const p = this.sectionConfigs[s.startingSectionIndex], f = document.createElement("div");
      f.className = "page-break-margin-top", f.style.height = `${s.marginTop}px`, p && (f.style.width = `${p.pageWidth}px`, f.style.margin = "0 auto");
      const b = this.resolveHeader(s.startingSectionIndex, s.startingPageInSection);
      b && p && this.renderHeaderFooterInZone(f, b, "header", p, i + 2, e), l.appendChild(f), this.overlay.appendChild(l);
    }
    if (this.sectionConfigs.length > 0) {
      const i = this.sectionConfigs.length - 1, s = this.sectionConfigs[i], a = t.length > 0 ? t[t.length - 1].startingPageInSection : 0, r = (n ?? this.canvas.scrollHeight) - s.marginBottom, l = document.createElement("div");
      l.className = "page-footer-overlay", l.style.top = `${r}px`, l.style.height = `${s.marginBottom}px`, l.style.width = `${s.pageWidth}px`, l.style.left = "50%", l.style.transform = "translateX(-50%)";
      const c = this.resolveFooter(i, a);
      c && this.renderHeaderFooterInZone(l, c, "footer", s, e, e), this.overlay.appendChild(l);
    }
  }
  /** Update the page frame min-height to encompass all content. */
  updateFrameHeight(t) {
    const n = this.canvas.scrollHeight, e = Math.max(this.config.pageHeight, t, n);
    this.pageFrame.style.minHeight = `${e}px`;
  }
  /** Fallback: render overlays from current DOM state after max iterations. */
  renderCurrentState() {
    const t = this.getAllBlockChildren(), n = [];
    for (const i of t)
      if (i.dataset.pageGap) {
        const s = i.closest("section"), a = s ? parseInt(s.dataset.sectionIndex ?? "0") : 0, r = this.sectionConfigs[a] ?? this.sectionConfigs[0], l = (r == null ? void 0 : r.marginBottom) ?? this.config.marginBottom, c = (r == null ? void 0 : r.marginTop) ?? this.config.marginTop, d = (r == null ? void 0 : r.pageWidth) ?? this.config.pageWidth, h = l + $ + c, u = parseFloat(i.style.marginTop) || h;
        n.push({
          y: i.offsetTop - u,
          marginBottom: l,
          marginTop: c,
          pageWidth: d,
          endingSectionIndex: a,
          endingPageInSection: 0,
          startingSectionIndex: a,
          startingPageInSection: 0
        });
      }
    const e = Array.from(this.canvas.children).filter(
      (i) => {
        var s;
        return ((s = i.tagName) == null ? void 0 : s.toLowerCase()) === "section";
      }
    );
    e.length > 0 && this.applySectionMinHeights(e, n), this.renderOverlays(n), this.updateFrameHeight(this.canvas.scrollHeight);
  }
  /**
   * Update page dimensions from document properties (twips from C# model).
   * Backwards-compatible wrapper for single-section documents.
   */
  updateFromDocProps(t) {
    t.pageWidth && (this.config.pageWidth = H(t.pageWidth)), t.pageHeight && (this.config.pageHeight = H(t.pageHeight)), t.marginTop && (this.config.marginTop = H(t.marginTop)), t.marginBottom && (this.config.marginBottom = H(t.marginBottom)), t.marginLeft && (this.config.marginLeft = H(t.marginLeft)), t.marginRight && (this.config.marginRight = H(t.marginRight)), this.canvas.style.width = `${this.config.pageWidth}px`, this.canvas.style.minHeight = `${this.config.pageHeight}px`, this.pageFrame.style.width = `${this.config.pageWidth}px`, this.pageFrame.style.minHeight = `${this.config.pageHeight}px`, this.pageFrame.style.padding = "0";
  }
  /**
   * Returns an array of gap zones in canvas-relative coordinates.
   * Each zone is a {top, bottom} range where the caret should not rest.
   */
  getGapZones() {
    const t = [], n = this.canvas.querySelectorAll("[data-page-gap]");
    for (const i of n) {
      const s = parseFloat(i.style.marginTop) || 0;
      if (s <= 0) continue;
      const a = i.offsetTop, r = a - s;
      t.push({ top: r, bottom: a });
    }
    const e = this.canvas.querySelectorAll("span[data-para-page-gap]");
    for (const i of e)
      t.push({ top: i.offsetTop, bottom: i.offsetTop + i.offsetHeight });
    return t;
  }
  /**
   * If the collapsed caret sits inside a page-break gap zone, snap it
   * to the nearest visible position above or below the gap.
   */
  adjustCursorForPageBreaks() {
    if (this.adjusting) return;
    const t = window.getSelection();
    if (!t || !t.isCollapsed || t.rangeCount === 0) return;
    const n = t.getRangeAt(0);
    if (!this.canvas.contains(n.startContainer)) return;
    const e = n.getBoundingClientRect();
    if (!e || e.top === 0 && e.bottom === 0) return;
    const i = this.canvas.getBoundingClientRect(), s = e.top - i.top + this.canvas.scrollTop, a = this.getGapZones();
    for (const r of a)
      if (s >= r.top && s < r.bottom) {
        const l = s - r.top, c = r.bottom - s, d = l <= c ? "up" : "down";
        this.adjusting = !0;
        try {
          this.moveCursorToVisiblePosition(
            d === "up" ? r.top - 1 : r.bottom + 1,
            d
          );
        } finally {
          this.adjusting = !1;
        }
        return;
      }
  }
  /**
   * Determine the 1-based page number currently visible at the top of the scroll area.
   */
  getCurrentPage(t) {
    const n = this.canvas.getBoundingClientRect(), i = t.getBoundingClientRect().top - n.top;
    for (let s = 0; s < this.breakPageBottomYPositions.length; s++)
      if (i < this.breakPageBottomYPositions[s])
        return s + 1;
    return this.pageCount;
  }
  /**
   * Returns the 1-based page number whose section should drive ruler dimensions.
   * Changes only when the full gap strip (bottom margin + gap + top margin) has
   * completely scrolled past the top of the viewport — i.e., the previous page
   * is no longer visible at all.
   */
  getPageForRuler(t) {
    const n = this.canvas.getBoundingClientRect(), i = t.getBoundingClientRect().top - n.top;
    for (let s = 0; s < this.breakBottomYPositions.length; s++)
      if (i < this.breakBottomYPositions[s])
        return s + 1;
    return this.pageCount;
  }
  /**
   * Returns the 1-based page where the text caret is currently located.
   * Uses DOM selection position, not scroll position.
   */
  getPageForCursor() {
    const t = window.getSelection();
    if (!t || t.rangeCount === 0) return 1;
    const n = t.getRangeAt(0);
    if (!this.canvas.contains(n.startContainer)) return 1;
    const e = n.getBoundingClientRect();
    if (!e || e.top === 0 && e.bottom === 0) return 1;
    const i = this.canvas.getBoundingClientRect(), s = e.top - i.top;
    for (let a = 0; a < this.breakPageBottomYPositions.length; a++)
      if (s < this.breakPageBottomYPositions[a]) return a + 1;
    return this.pageCount;
  }
  /** Returns the 0-based section index where the text caret is currently located. */
  getSectionForCursor() {
    const t = this.getPageForCursor();
    return this.pageSectionMap[t - 1] ?? 0;
  }
  /**
   * Returns the ruler dimensions for the section containing the given 1-based page number.
   * All values are already in px (converted by updateFromSections).
   */
  getPageRulerDimensions(t) {
    if (this.sectionConfigs.length === 0) return null;
    const n = this.pageSectionMap[t - 1] ?? 0, e = this.sectionConfigs[n] ?? this.sectionConfigs[0];
    return e ? {
      pageWidth: e.pageWidth,
      pageHeight: e.pageHeight,
      marginLeft: e.marginLeft,
      marginRight: e.marginRight,
      marginTop: e.marginTop,
      marginBottom: e.marginBottom
    } : null;
  }
  /**
   * Returns the scroll-y of the given 1-based page's top edge (start of its
   * white paper area, just after the gray inter-page gap).
   * Uses the real break positions from the last pagination run — correct even
   * when different sections have different page heights.
   */
  getPageTopScrollY(t) {
    if (t <= 1) return 20;
    const e = t - 2;
    return e < this.breakPageBottomYPositions.length ? this.breakPageBottomYPositions[e] + $ + 20 : 20;
  }
  /**
   * Total scrollable height of the editor (canvas content + pages-wrapper padding).
   * Use this for sizing the vertical ruler SVG accurately.
   */
  getTotalScrollHeight() {
    return this.canvas.scrollHeight + 40;
  }
  /**
   * Move the caret to a visible position at the given canvas-relative Y coordinate.
   */
  moveCursorToVisiblePosition(t, n) {
    const e = this.canvas.getBoundingClientRect(), i = t - this.canvas.scrollTop + e.top, s = e.left + e.width / 2, a = n === "up" ? -2 : 2, r = n === "up" ? e.top : e.bottom;
    for (let l = i; n === "up" ? l >= r : l <= r; l += a)
      if (typeof document.caretRangeFromPoint == "function") {
        const c = document.caretRangeFromPoint(s, l);
        if (c && this.canvas.contains(c.startContainer)) {
          const d = window.getSelection();
          d && (d.removeAllRanges(), d.addRange(c));
          return;
        }
      }
  }
}
function $t(o, t = 14) {
  const n = "http://www.w3.org/2000/svg", e = document.createElementNS(n, "svg");
  e.setAttribute("width", String(t)), e.setAttribute("height", String(t)), e.setAttribute("viewBox", "0 0 24 24"), e.setAttribute("fill", "none"), e.setAttribute("stroke", "currentColor"), e.setAttribute("stroke-width", "1.75"), e.setAttribute("stroke-linecap", "round"), e.setAttribute("stroke-linejoin", "round"), e.style.pointerEvents = "none", e.style.flexShrink = "0";
  for (const [i, s] of o) {
    const a = document.createElementNS(n, i);
    for (const [r, l] of Object.entries(s))
      a.setAttribute(r, l);
    e.appendChild(a);
  }
  return e;
}
function Ot(o) {
  return (o / 1440).toFixed(2);
}
function it(o) {
  return (o / 96).toFixed(2);
}
function P(o, t) {
  return document.createElement(o);
}
class Ge {
  constructor(t, n) {
    /** Width-transition outer wrapper (0 → 220px). Inserted into editorRow. */
    g(this, "wrapper");
    /** Fixed-width 220px content pane. */
    g(this, "inner");
    g(this, "body");
    g(this, "tabRow");
    g(this, "isOpen", !1);
    g(this, "currentSection", 0);
    g(this, "snapshots", []);
    g(this, "canvas");
    this.canvas = n, this.wrapper = t, this.wrapper.style.cssText = "overflow:hidden;width:0;transition:width 200ms ease;flex-shrink:0;border-left:1px solid #e5e7eb;", this.inner = P("div"), this.inner.style.cssText = "width:260px;height:100%;display:flex;flex-direction:column;background:#f9fafb;font-size:12px;", this.wrapper.appendChild(this.inner);
    const e = P("div");
    e.style.cssText = "display:flex;align-items:center;justify-content:space-between;padding:6px 8px;background:white;border-bottom:1px solid #e5e7eb;flex-shrink:0;";
    const i = P("span");
    i.style.cssText = "font-weight:600;color:#374151;display:flex;align-items:center;gap:4px;", i.appendChild($t(jt, 12)), i.append(" Margins");
    const s = P("button");
    s.type = "button", s.style.cssText = "display:flex;align-items:center;color:#9ca3af;padding:2px;border-radius:3px;cursor:pointer;background:none;border:none;", s.appendChild($t(be, 12)), s.addEventListener("click", () => this.close()), e.appendChild(i), e.appendChild(s), this.inner.appendChild(e), this.tabRow = P("div"), this.tabRow.style.cssText = "display:none;padding:4px 8px;gap:4px;background:white;border-bottom:1px solid #e5e7eb;flex-shrink:0;flex-wrap:wrap;", this.inner.appendChild(this.tabRow), this.body = P("div"), this.body.style.cssText = "flex:1;overflow-y:auto;padding:8px;font-family:ui-monospace,monospace;", this.inner.appendChild(this.body);
  }
  /** Returns the wrapper element (already inserted by caller). */
  getElement() {
    return this.wrapper;
  }
  toggle() {
    this.isOpen ? this.close() : this.open();
  }
  open() {
    this.isOpen = !0, this.wrapper.style.width = "260px", this.render();
  }
  close() {
    this.isOpen = !1, this.wrapper.style.width = "0";
  }
  /** Called when cursor moves; updates the displayed section if it changed. */
  setCursorSection(t) {
    t !== this.currentSection && (this.currentSection = t, this.isOpen && this.render());
  }
  update(t) {
    this.snapshots = t, this.currentSection >= t.length && (this.currentSection = 0), this.isOpen && this.render();
  }
  // ── Private ────────────────────────────────────────────────────────────────
  render() {
    const t = this.snapshots[this.currentSection];
    if (this.tabRow.innerHTML = "", this.snapshots.length > 1 ? (this.tabRow.style.display = "flex", this.snapshots.forEach((e, i) => {
      const s = P("button");
      s.type = "button", s.textContent = `§${i + 1}`, s.style.cssText = "padding:2px 6px;border-radius:3px;font-size:11px;cursor:pointer;border:1px solid #d1d5db;" + (i === this.currentSection ? "background:#3b82f6;color:white;border-color:#3b82f6;" : "background:white;color:#374151;"), s.addEventListener("click", () => {
        this.currentSection = i, this.render();
      }), this.tabRow.appendChild(s);
    })) : this.tabRow.style.display = "none", this.body.innerHTML = "", !t) {
      const e = P("div");
      e.style.color = "#9ca3af", e.textContent = "No section data.", this.body.appendChild(e);
      return;
    }
    const n = this.measureSection(this.currentSection);
    this.body.appendChild(this.sectionLabel("Page")), this.body.appendChild(this.row(
      `${t.pxPageWidth} × ${t.pxPageHeight} px`,
      `${it(t.pxPageWidth)}" × ${it(t.pxPageHeight)}"`
    )), this.body.appendChild(this.sectionLabel("Margins")), this.body.appendChild(this.marginRow("Top", t.rawMarginTop, t.pxMarginTop, n["padding-top"])), this.body.appendChild(this.marginRow("Bottom", t.rawMarginBottom, t.pxMarginBottom, n["padding-bottom"])), this.body.appendChild(this.marginRow("Left", t.rawMarginLeft, t.pxMarginLeft, n["padding-left"])), this.body.appendChild(this.marginRow("Right", t.rawMarginRight, t.pxMarginRight, n["padding-right"])), this.body.appendChild(this.sectionLabel("Header")), this.body.appendChild(this.twipRow("Distance", t.rawHeaderDistance, t.pxHeaderDistance)), this.body.appendChild(this.pxRow("Zone height", t.pxMarginTop)), this.body.appendChild(this.pxRow("Content zone", t.pxMarginTop - t.pxHeaderDistance)), this.body.appendChild(this.sectionLabel("Footer")), this.body.appendChild(this.twipRow("Distance", t.rawFooterDistance, t.pxFooterDistance)), this.body.appendChild(this.pxRow("Zone height", t.pxMarginBottom)), this.body.appendChild(this.pxRow("Content zone", t.pxMarginBottom - t.pxFooterDistance)), this.body.appendChild(this.sectionLabel("Content Area")), this.body.appendChild(this.pxRow("Width", t.pxContentWidth)), this.body.appendChild(this.pxRow("Height", t.pxContentHeight)), this.body.appendChild(this.sectionLabel("DOM Measured")), this.body.appendChild(this.domCheckRow("padding-top", t.pxMarginTop, n["padding-top"])), this.body.appendChild(this.domCheckRow("padding-bottom", t.pxMarginBottom, n["padding-bottom"])), this.body.appendChild(this.domCheckRow("padding-left", t.pxMarginLeft, n["padding-left"])), this.body.appendChild(this.domCheckRow("padding-right", t.pxMarginRight, n["padding-right"]));
  }
  /** Read computed styles on the nth <section> inside the canvas. */
  measureSection(t) {
    const n = this.canvas.querySelectorAll("section"), e = n[t] ?? n[0];
    if (!e) return {};
    const i = getComputedStyle(e);
    return {
      "padding-top": parseFloat(i.paddingTop) || 0,
      "padding-bottom": parseFloat(i.paddingBottom) || 0,
      "padding-left": parseFloat(i.paddingLeft) || 0,
      "padding-right": parseFloat(i.paddingRight) || 0
    };
  }
  // ── Row builders ───────────────────────────────────────────────────────────
  sectionLabel(t) {
    const n = P("div");
    return n.style.cssText = "color:#9ca3af;text-transform:uppercase;letter-spacing:0.05em;font-size:10px;margin-top:10px;margin-bottom:2px;border-bottom:1px solid #e5e7eb;padding-bottom:2px;", n.textContent = t, n;
  }
  row(t, n) {
    const e = P("div");
    e.style.cssText = "display:flex;justify-content:space-between;padding:1px 0;";
    const i = P("span");
    i.style.color = "#1f2937", i.textContent = t;
    const s = P("span");
    return s.style.color = "#6b7280", s.textContent = n, e.appendChild(i), e.appendChild(s), e;
  }
  /** twips → px → inches row, with optional DOM comparison. */
  marginRow(t, n, e, i) {
    const s = P("div");
    s.style.cssText = "padding:1px 0;";
    const a = P("div");
    a.style.cssText = "display:flex;justify-content:space-between;";
    const r = P("span");
    r.style.color = "#374151", r.textContent = t;
    const l = P("span");
    if (l.style.cssText = "color:#1f2937;font-weight:500;white-space:nowrap;", l.textContent = `${n} tw → ${e} px → ${Ot(n)}"`, a.appendChild(r), a.appendChild(l), s.appendChild(a), i !== void 0) {
      const c = Math.abs(i - e) > 0.5, d = P("div");
      d.style.cssText = "display:flex;justify-content:flex-end;font-size:10px;";
      const h = P("span");
      h.style.color = c ? "#f97316" : "#16a34a", h.textContent = c ? `DOM: ${i}px ⚠` : "✓", d.appendChild(h), s.appendChild(d);
    }
    return s;
  }
  twipRow(t, n, e) {
    const i = P("div");
    i.style.cssText = "display:flex;justify-content:space-between;padding:1px 0;";
    const s = P("span");
    s.style.color = "#374151", s.textContent = t;
    const a = P("span");
    return a.style.cssText = "color:#1f2937;white-space:nowrap;", a.textContent = `${n} tw → ${e} px → ${Ot(n)}"`, i.appendChild(s), i.appendChild(a), i;
  }
  pxRow(t, n) {
    const e = P("div");
    e.style.cssText = "display:flex;justify-content:space-between;padding:1px 0;";
    const i = P("span");
    i.style.color = "#374151", i.textContent = t;
    const s = P("span");
    return s.style.cssText = "color:#1f2937;white-space:nowrap;", s.textContent = `${n} px (${it(n)}")`, e.appendChild(i), e.appendChild(s), e;
  }
  domCheckRow(t, n, e) {
    const i = P("div");
    i.style.cssText = "display:flex;justify-content:space-between;padding:1px 0;";
    const s = P("span");
    s.style.color = "#374151", s.textContent = t;
    const a = P("span"), r = Math.abs(e - n) > 0.5;
    return a.style.cssText = r ? "color:#f97316;font-weight:600;white-space:nowrap;" : "color:#16a34a;white-space:nowrap;", a.textContent = r ? `${e}px ⚠` : `${e}px ✓`, i.appendChild(s), i.appendChild(a), i;
  }
}
const Z = 9525;
let V, K, _, O = null;
function Ue(o, t, n, e) {
  V = n, K = e, _ = t, getComputedStyle(t).position === "static" && (t.style.position = "relative"), o.addEventListener("click", Xe), o.addEventListener("mousedown", (i) => {
    i.target.closest('[data-type="image"]') && i.preventDefault();
  }), document.addEventListener("click", qe, !0);
}
function Ye() {
  U();
}
function Xe(o) {
  const t = o.target.closest('[data-type="image"]');
  t && (o.stopPropagation(), Ze(t));
}
function qe(o) {
  if (!O) return;
  const t = o.target;
  t === O.imgEl || O.overlayEl.contains(t) || U();
}
function Ze(o) {
  U();
  const t = o.dataset.nodeId ?? "";
  if (!t) return;
  const n = document.createElement("div");
  n.className = "wave-img-handles", n.style.cssText = "position:absolute;pointer-events:none;z-index:100;";
  const e = document.createElement("div");
  e.className = "wave-img-selected-border", n.appendChild(e);
  const i = ["nw", "n", "ne", "e", "se", "s", "sw", "w"], s = /* @__PURE__ */ new Map();
  for (const c of i) {
    const d = document.createElement("div");
    d.className = "wave-img-handle", d.dataset.pos = c, d.style.cursor = Qe(c), n.appendChild(d), s.set(c, d), Ve(d, c, o, t);
  }
  const a = document.createElement("div");
  a.className = "wave-img-move-zone", n.appendChild(a), Je(a, o, t);
  const r = document.createElement("div");
  r.className = "wave-img-rotate-line", n.appendChild(r);
  const l = document.createElement("div");
  l.className = "wave-img-handle", l.dataset.pos = "rotate", l.style.cursor = "grab", n.appendChild(l), s.set("rotate", l), Ke(l, o, t, n), _.appendChild(n), O = { imgEl: o, nodeId: t, overlayEl: n, handles: s }, de();
}
function U() {
  O && (O.overlayEl.remove(), O = null);
}
function de(o, t) {
  if (!O) return;
  const { imgEl: n, overlayEl: e, handles: i } = O, s = parseFloat(n.dataset.rotation ?? "0"), a = _.getBoundingClientRect(), r = n.getBoundingClientRect(), l = parseFloat(n.dataset.origWidth ?? "0") || n.offsetWidth, c = parseFloat(n.dataset.origHeight ?? "0") || n.offsetHeight, d = o ?? l, h = t ?? c, u = r.left + r.width / 2 - a.left + _.scrollLeft, p = r.top + r.height / 2 - a.top + _.scrollTop;
  e.style.left = `${u - d / 2}px`, e.style.top = `${p - h / 2}px`, e.style.width = `${d}px`, e.style.height = `${h}px`, e.style.transform = s !== 0 ? `rotate(${s}deg)` : "", e.style.transformOrigin = "50% 50%";
  const f = {
    nw: ["0%", "0%"],
    n: ["50%", "0%"],
    ne: ["100%", "0%"],
    e: ["100%", "50%"],
    se: ["100%", "100%"],
    s: ["50%", "100%"],
    sw: ["0%", "100%"],
    w: ["0%", "50%"]
  };
  for (const [m, [x, C]] of Object.entries(f)) {
    const v = i.get(m);
    v && (v.style.left = x, v.style.top = C);
  }
  const b = 28, w = i.get("rotate"), k = e.querySelector(".wave-img-rotate-line");
  w && (w.style.left = "50%", w.style.top = `${-b}px`), k && (k.style.left = `${d / 2}px`, k.style.top = `${-b}px`, k.style.height = `${b}px`);
}
function Ve(o, t, n, e) {
  o.addEventListener("mousedown", (i) => {
    i.preventDefault(), i.stopPropagation();
    const s = i.clientX, a = i.clientY, r = parseFloat(n.dataset.origWidth ?? "0") || n.offsetWidth, l = parseFloat(n.dataset.origHeight ?? "0") || n.offsetHeight, c = r / l;
    let d = r, h = l;
    function u(f) {
      const b = f.clientX - s, w = f.clientY - a, k = f.shiftKey;
      let m = r, x = l;
      t.includes("e") && (m = Math.max(16, r + b)), t.includes("w") && (m = Math.max(16, r - b)), t.includes("s") && (x = Math.max(16, l + w)), t.includes("n") && (x = Math.max(16, l - w)), k && (Math.abs(b) >= Math.abs(w) ? x = m / c : m = x * c), d = m, h = x, de(d, h);
    }
    function p() {
      document.removeEventListener("mousemove", u), document.removeEventListener("mouseup", p);
      const f = Math.round(d * Z), b = Math.round(h * Z);
      V.setImageSize(e, f, b).then((w) => {
        K(w), U();
      });
    }
    document.addEventListener("mousemove", u), document.addEventListener("mouseup", p);
  });
}
function Ke(o, t, n, e) {
  o.addEventListener("mousedown", (i) => {
    i.preventDefault(), i.stopPropagation();
    const s = t.getBoundingClientRect(), a = s.left + s.width / 2, r = s.top + s.height / 2, l = Math.atan2(i.clientY - r, i.clientX - a), c = parseFloat(t.dataset.rotation ?? "0");
    let d = c;
    o.style.cursor = "grabbing";
    function h(p) {
      const b = (Math.atan2(p.clientY - r, p.clientX - a) - l) * (180 / Math.PI);
      let w = c + b;
      p.shiftKey && (w = Math.round(w / 15) * 15), d = w, e.style.transform = `rotate(${w}deg)`, e.style.transformOrigin = "50% 50%";
    }
    function u() {
      document.removeEventListener("mousemove", h), document.removeEventListener("mouseup", u), o.style.cursor = "grab", e.style.transform = "", e.style.transformOrigin = "", V.setImageRotation(n, d).then((p) => {
        K(p), U();
      });
    }
    document.addEventListener("mousemove", h), document.addEventListener("mouseup", u);
  });
}
function Je(o, t, n) {
  o.addEventListener("mousedown", (e) => {
    if (e.button !== 0 || (e.preventDefault(), e.stopPropagation(), !O)) return;
    const { overlayEl: i } = O, s = parseFloat(i.style.left) || 0, a = parseFloat(i.style.top) || 0, r = e.clientX, l = e.clientY;
    let c = s, d = a;
    i.style.cursor = "grabbing";
    function h(p) {
      c = s + (p.clientX - r), d = a + (p.clientY - l), i.style.left = `${c}px`, i.style.top = `${d}px`;
    }
    function u() {
      document.removeEventListener("mousemove", h), document.removeEventListener("mouseup", u), i.style.cursor = "";
      const p = t.closest(".editor-canvas") ?? document.querySelector(".editor-canvas"), f = _.getBoundingClientRect(), b = p.getBoundingClientRect(), w = b.left - f.left + _.scrollLeft, k = b.top - f.top + _.scrollTop, m = c - w, x = d - k, C = Math.round(Math.max(0, m) * Z), v = Math.round(Math.max(0, x) * Z);
      V.setImagePosition(n, C, v).then((L) => {
        K(L), U();
      });
    }
    document.addEventListener("mousemove", h), document.addEventListener("mouseup", u);
  });
}
function Qe(o) {
  switch (o) {
    case "nw":
    case "se":
      return "nwse-resize";
    case "ne":
    case "sw":
      return "nesw-resize";
    case "n":
    case "s":
      return "ns-resize";
    case "e":
    case "w":
      return "ew-resize";
    default:
      return "grab";
  }
}
const tn = [
  { label: "Inline", value: "inline" },
  { label: "Float Left", value: "floatleft" },
  { label: "Float Right", value: "floatright" },
  { label: "Break Text", value: "topandbottom" },
  { label: "Behind Text", value: "behindtext" },
  { label: "In Front of Text", value: "infrontoftext" }
];
async function Wt(o) {
  const n = await (await fetch(o.src)).blob();
  await navigator.clipboard.write([new ClipboardItem({ [n.type]: n })]);
}
function zt() {
  var o;
  return ((o = window.getSelection()) == null ? void 0 : o.toString()) ?? "";
}
function en(o) {
  const t = o.anchor, n = o.focus;
  return t.blockIndex === n.blockIndex && t.inlineIndex === n.inlineIndex && t.offset === n.offset;
}
class nn {
  constructor(t, n, e) {
    g(this, "canvas");
    g(this, "engine");
    g(this, "onResponse");
    g(this, "menuEl");
    // Bound handlers stored for cleanup
    g(this, "_onContextMenu");
    g(this, "_onDocContextMenu");
    g(this, "_onClickOutside");
    g(this, "_onKeyDown");
    this.canvas = t, this.engine = n, this.onResponse = e, this.menuEl = document.createElement("div"), this.menuEl.id = "wave-context-menu", this.menuEl.className = "fixed z-50 min-w-44 rounded-lg shadow-lg border border-gray-200 bg-white py-1 text-sm text-gray-700", this.menuEl.style.display = "none", document.body.appendChild(this.menuEl), this._onContextMenu = this.handleContextMenu.bind(this), this._onDocContextMenu = this.handleDocContextMenu.bind(this), this._onClickOutside = this.handleClickOutside.bind(this), this._onKeyDown = this.handleKeyDown.bind(this), t.addEventListener("contextmenu", this._onContextMenu), document.addEventListener("contextmenu", this._onDocContextMenu, !0), document.addEventListener("click", this._onClickOutside, !0), document.addEventListener("keydown", this._onKeyDown);
  }
  destroy() {
    this.canvas.removeEventListener("contextmenu", this._onContextMenu), document.removeEventListener("contextmenu", this._onDocContextMenu, !0), document.removeEventListener("click", this._onClickOutside, !0), document.removeEventListener("keydown", this._onKeyDown), this.menuEl.remove();
  }
  // ─── Event handlers ────────────────────────────────────────
  handleContextMenu(t) {
    t.preventDefault();
    const n = t.target, e = n.tagName === "IMG" && n.dataset.type === "image" ? n : null, i = (e == null ? void 0 : e.dataset.nodeId) ?? null, s = (e == null ? void 0 : e.dataset.wrapMode) ?? null, a = T(this.canvas);
    this.buildMenu(e, i, s, a, t.clientX, t.clientY);
  }
  handleDocContextMenu(t) {
    this.canvas.contains(t.target) || this.hide();
  }
  handleClickOutside(t) {
    this.menuEl.contains(t.target) || this.hide();
  }
  handleKeyDown(t) {
    t.key === "Escape" && this.menuEl.style.display !== "none" && this.hide();
  }
  // ─── Menu construction ─────────────────────────────────────
  buildMenu(t, n, e, i, s, a) {
    this.menuEl.innerHTML = "";
    const r = !!(i && !en(i)), l = !!(t && n);
    if (this.addItem("Cut", !(l || r), async () => {
      if (l && t && n) {
        await Wt(t).catch(() => {
        });
        const c = await this.engine.deleteImageRun(n);
        this.onResponse(c);
      } else if (r && i) {
        await navigator.clipboard.writeText(zt()).catch(() => {
        });
        const c = await this.engine.deleteSelection(i);
        this.onResponse(c);
      }
      this.hide();
    }), this.addItem("Copy", !(l || r), async () => {
      l && t ? await Wt(t).catch(() => {
      }) : r && await navigator.clipboard.writeText(zt()).catch(() => {
      }), this.hide();
    }), this.addItem("Paste", !1, async () => {
      try {
        const c = await navigator.clipboard.readText();
        if (c && i) {
          const d = await this.engine.pasteText(c, i);
          this.onResponse(d);
        }
      } catch {
      }
      this.hide();
    }), this.addItem("Paste without formatting", !1, async () => {
      try {
        const c = await navigator.clipboard.readText();
        if (c && i) {
          const d = await this.engine.pasteText(c, i);
          this.onResponse(d);
        }
      } catch {
      }
      this.hide();
    }), this.addItem("Delete", !(l || r), async () => {
      if (l && n) {
        const c = await this.engine.deleteImageRun(n);
        this.onResponse(c);
      } else if (r && i) {
        const c = await this.engine.deleteSelection(i);
        this.onResponse(c);
      }
      this.hide();
    }), l && n) {
      this.addSeparator();
      for (const { label: c, value: d } of tn) {
        const h = e === d;
        this.addItem(
          (h ? "✓ " : "   ") + c,
          !1,
          async () => {
            const u = await this.engine.setImageWrapMode(n, d);
            this.onResponse(u), this.hide();
          }
        );
      }
    }
    this.menuEl.style.display = "block", this.menuEl.style.left = "-9999px", this.menuEl.style.top = "-9999px", requestAnimationFrame(() => {
      this.position(s, a);
    });
  }
  // ─── Helpers ───────────────────────────────────────────────
  addItem(t, n, e) {
    const i = document.createElement("div");
    i.textContent = t, i.className = [
      "flex items-center gap-2 px-3 py-1.5 select-none rounded-sm mx-1",
      n ? "opacity-40 cursor-default pointer-events-none" : "hover:bg-gray-100 cursor-pointer"
    ].join(" "), n || (i.addEventListener("mousedown", (s) => s.preventDefault()), i.addEventListener("click", () => e())), this.menuEl.appendChild(i);
  }
  addSeparator() {
    const t = document.createElement("div");
    t.className = "border-t border-gray-200 my-1", this.menuEl.appendChild(t);
  }
  position(t, n) {
    const e = this.menuEl.getBoundingClientRect(), i = window.innerWidth, s = window.innerHeight, a = Math.min(t, i - e.width - 4), r = Math.min(n, s - e.height - 4);
    this.menuEl.style.left = `${Math.max(0, a)}px`, this.menuEl.style.top = `${Math.max(0, r)}px`;
  }
  hide() {
    this.menuEl.style.display = "none";
  }
}
function on(o) {
  document.addEventListener("selectionchange", () => sn(o));
}
function sn(o) {
  const t = window.getSelection();
  if (!t || t.rangeCount === 0) return;
  const n = t.anchorNode;
  if (!n || !o.contains(n)) return;
  const e = n.nodeType === Node.TEXT_NODE ? n.parentElement : n, i = an(e, o);
  o.style.caretColor = i && rn(i) ? "white" : "";
}
function an(o, t) {
  let n = o;
  for (; n && n !== t.parentElement; ) {
    const e = getComputedStyle(n).backgroundColor;
    if (e && e !== "transparent" && e !== "rgba(0, 0, 0, 0)")
      return e;
    n = n.parentElement;
  }
  return null;
}
function rn(o) {
  const t = o.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
  if (!t) return !1;
  const [n, e, i] = [+t[1] / 255, +t[2] / 255, +t[3] / 255];
  return 0.2126 * ot(n) + 0.7152 * ot(e) + 0.0722 * ot(i) < 0.4;
}
function ot(o) {
  return o <= 0.04045 ? o / 12.92 : Math.pow((o + 0.055) / 1.055, 2.4);
}
class ln {
  constructor(t, n, e) {
    g(this, "engine");
    g(this, "canvas");
    g(this, "onResponse");
    g(this, "processing", !1);
    g(this, "handleBeforeInput", async (t) => {
      var i, s;
      if (t.isComposing || (t.preventDefault(), this.processing)) return;
      const n = T(this.canvas);
      if (!n) return;
      this.processing = !0;
      let e = null;
      try {
        switch (t.inputType) {
          case "insertText":
            t.data && (e = await this.engine.insertText(t.data, n));
            break;
          case "insertParagraph":
            e = await this.engine.splitParagraph(n);
            break;
          case "insertLineBreak":
            e = await this.engine.insertBreak("textwrapping", n);
            break;
          case "deleteContentBackward":
          case "deleteSoftLineBackward":
          case "deleteWordBackward":
            n.anchor.blockIndex === n.focus.blockIndex && n.anchor.inlineIndex === n.focus.inlineIndex && n.anchor.offset === n.focus.offset ? e = await this.engine.deleteBackward(n) : e = await this.engine.deleteSelection(n);
            break;
          case "deleteContentForward":
          case "deleteSoftLineForward":
          case "deleteWordForward":
            n.anchor.blockIndex === n.focus.blockIndex && n.anchor.inlineIndex === n.focus.inlineIndex && n.anchor.offset === n.focus.offset ? e = await this.engine.deleteForward(n) : e = await this.engine.deleteSelection(n);
            break;
          case "insertFromPaste": {
            const a = (i = t.dataTransfer) == null ? void 0 : i.getData("text/plain");
            a && (e = await this.engine.pasteText(a, n));
            break;
          }
          case "insertFromDrop": {
            const a = (s = t.dataTransfer) == null ? void 0 : s.getData("text/plain");
            a && (e = await this.engine.pasteText(a, n));
            break;
          }
          case "formatBold":
            e = await this.engine.toggleFormat("bold", n);
            break;
          case "formatItalic":
            e = await this.engine.toggleFormat("italic", n);
            break;
          case "formatUnderline":
            e = await this.engine.toggleFormat("underline", n);
            break;
          case "formatStrikeThrough":
            e = await this.engine.toggleFormat("strikethrough", n);
            break;
          case "historyUndo":
            e = await this.engine.undo();
            break;
          case "historyRedo":
            e = await this.engine.redo();
            break;
        }
      } finally {
        this.processing = !1;
      }
      e && this.onResponse(e);
    });
    g(this, "handleCompositionEnd", async (t) => {
      const n = t.data;
      if (!n) return;
      const e = T(this.canvas);
      if (!e) return;
      const i = await this.engine.insertText(n, e);
      this.onResponse(i);
    });
    this.engine = t, this.canvas = n, this.onResponse = e, this.canvas.addEventListener("beforeinput", this.handleBeforeInput), this.canvas.addEventListener("compositionend", this.handleCompositionEnd), on(n);
  }
  destroy() {
    this.canvas.removeEventListener("beforeinput", this.handleBeforeInput), this.canvas.removeEventListener("compositionend", this.handleCompositionEnd);
  }
}
class cn {
  constructor(t, n, e) {
    g(this, "engine");
    g(this, "canvas");
    g(this, "onResponse");
    g(this, "shortcuts");
    g(this, "handleKeyDown", async (t) => {
      const n = t.ctrlKey || t.metaKey;
      if (n && !t.shiftKey && !t.altKey && t.key === "Enter") {
        t.preventDefault();
        const e = T(this.canvas);
        if (e) {
          const i = await this.engine.insertBreak("page", e);
          i && this.onResponse(i);
        }
        return;
      }
      for (const e of this.shortcuts) {
        const i = e.ctrl ? n : !n, s = e.shift ? t.shiftKey : !t.shiftKey;
        if (t.key.toLowerCase() === e.key.toLowerCase() && i && s) {
          t.preventDefault();
          const r = await e.handler();
          r && this.onResponse(r);
          return;
        }
      }
    });
    this.engine = t, this.canvas = n, this.onResponse = e, this.shortcuts = this.buildShortcuts(), this.canvas.addEventListener("keydown", this.handleKeyDown);
  }
  destroy() {
    this.canvas.removeEventListener("keydown", this.handleKeyDown);
  }
  buildShortcuts() {
    return [
      // Formatting
      { key: "b", ctrl: !0, handler: () => this.formatCmd("bold") },
      { key: "i", ctrl: !0, handler: () => this.formatCmd("italic") },
      { key: "u", ctrl: !0, handler: () => this.formatCmd("underline") },
      // History
      { key: "z", ctrl: !0, handler: () => this.engine.undo() },
      { key: "z", ctrl: !0, shift: !0, handler: () => this.engine.redo() },
      { key: "y", ctrl: !0, handler: () => this.engine.redo() },
      // Alignment
      { key: "l", ctrl: !0, handler: () => this.alignCmd("left") },
      { key: "e", ctrl: !0, handler: () => this.alignCmd("center") },
      { key: "r", ctrl: !0, handler: () => this.alignCmd("right") },
      { key: "j", ctrl: !0, handler: () => this.alignCmd("both") },
      // Indent
      {
        key: "Tab",
        handler: () => {
          const t = T(this.canvas);
          return t ? this.engine.setIndent(720, 0, t) : Promise.resolve(null);
        }
      },
      {
        key: "Tab",
        shift: !0,
        handler: () => {
          const t = T(this.canvas);
          return t ? this.engine.setIndent(-720, 0, t) : Promise.resolve(null);
        }
      }
    ];
  }
  async formatCmd(t) {
    const n = T(this.canvas);
    return n ? this.engine.toggleFormat(t, n) : null;
  }
  async alignCmd(t) {
    const n = T(this.canvas);
    return n ? this.engine.setAlignment(t, n) : null;
  }
}
class dn {
  constructor(t, n, e) {
    g(this, "engine");
    g(this, "canvas");
    g(this, "onResponse");
    g(this, "handlePaste", async (t) => {
      var s;
      t.preventDefault();
      const n = (s = t.clipboardData) == null ? void 0 : s.getData("text/plain");
      if (!n) return;
      const e = T(this.canvas);
      if (!e) return;
      const i = await this.engine.pasteText(n, e);
      this.onResponse(i);
    });
    this.engine = t, this.canvas = n, this.onResponse = e, this.canvas.addEventListener("paste", this.handlePaste);
  }
  destroy() {
    this.canvas.removeEventListener("paste", this.handlePaste);
  }
}
function st() {
  var e;
  const o = window.getSelection();
  if (!o || o.rangeCount === 0) return null;
  const t = o.anchorNode, n = (t == null ? void 0 : t.nodeType) === Node.TEXT_NODE ? t.parentElement : t;
  return ((e = n == null ? void 0 : n.closest("td[data-node-id]")) == null ? void 0 : e.dataset.nodeId) ?? null;
}
const D = {
  undo: async ({ engine: o, onResponse: t }) => {
    const n = await o.undo();
    t(n);
  },
  redo: async ({ engine: o, onResponse: t }) => {
    const n = await o.redo();
    t(n);
  },
  bold: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.toggleFormat("bold", e);
    n(i);
  },
  italic: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.toggleFormat("italic", e);
    n(i);
  },
  underline: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.toggleFormat("underline", e);
    n(i);
  },
  strikethrough: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.toggleFormat("strikethrough", e);
    n(i);
  },
  alignLeft: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.setAlignment("left", e);
    n(i);
  },
  alignCenter: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.setAlignment("center", e);
    n(i);
  },
  alignRight: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.setAlignment("right", e);
    n(i);
  },
  alignJustify: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.setAlignment("both", e);
    n(i);
  },
  bullet: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.toggleList("bullet", e);
    n(i);
  },
  numbered: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.toggleList("numbered", e);
    n(i);
  },
  insertTable: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.insertTable(3, 3, e);
    n(i);
  },
  insertLink: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = prompt("Enter URL:");
    if (!e) return;
    const i = prompt("Link text:", e) || e, s = T(t);
    if (!s) return;
    const a = await o.insertHyperlink(e, i, s);
    n(a);
  },
  cellBorders: async ({ engine: o, canvas: t, onResponse: n }, e) => {
    if (!e) return;
    const i = st();
    if (!i) return;
    const s = T(t);
    if (!s) return;
    const r = { none: 4, thin: 4, medium: 8, thick: 12 }[e] ?? 4, l = e === "none" ? null : {
      top: { style: "single", size: r, color: "auto" },
      bottom: { style: "single", size: r, color: "auto" },
      left: { style: "single", size: r, color: "auto" },
      right: { style: "single", size: r, color: "auto" }
    }, c = await o.setTableCellBorders(i, l, s);
    n(c);
  },
  cellBackground: async ({ engine: o, canvas: t, onResponse: n }) => {
    let e = document.getElementById("wave-cell-bg-input");
    e || (e = document.createElement("input"), e.id = "wave-cell-bg-input", e.type = "color", e.value = "#ffffff", e.style.cssText = "position:fixed;width:0;height:0;opacity:0;pointer-events:none", document.body.appendChild(e));
    const i = e, s = async () => {
      i.removeEventListener("change", s);
      const a = st();
      if (!a) return;
      const r = T(t);
      if (!r) return;
      const l = await o.setTableCellShading(
        a,
        i.value.replace("#", ""),
        r
      );
      n(l), t.focus();
    };
    e.addEventListener("change", s), e.click();
  },
  removeCellBackground: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = st();
    if (!e) return;
    const i = T(t);
    if (!i) return;
    const s = await o.setTableCellShading(e, null, i);
    n(s);
  },
  sectionBreak: async ({ engine: o, canvas: t, onResponse: n }, e) => {
    if (!e) return;
    const i = T(t);
    if (!i) return;
    const s = await o.insertSectionBreak(e, i);
    n(s);
  },
  portrait: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.setPageOrientation("portrait", e);
    n(i);
  },
  landscape: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = T(t);
    if (!e) return;
    const i = await o.setPageOrientation("landscape", e);
    n(i);
  },
  columns: async ({ engine: o, canvas: t, onResponse: n }, e) => {
    if (!e) return;
    const i = T(t);
    if (!i) return;
    const s = await o.setColumns(parseInt(e), 720, i);
    n(s);
  },
  paragraphStyle: async ({ engine: o, canvas: t, onResponse: n }, e) => {
    if (!e) return;
    const i = T(t);
    if (!i) return;
    const s = await o.setParagraphStyle(e, i);
    n(s);
  },
  fontFamily: async ({ engine: o, canvas: t, onResponse: n }, e) => {
    if (!(e != null && e.trim())) return;
    ie([e.trim()]);
    const i = T(t);
    if (!i) return;
    const s = await o.setFontFamily(e.trim(), i);
    n(s);
  },
  fontSize: async ({ engine: o, canvas: t, onResponse: n }, e) => {
    const i = parseFloat(e ?? "");
    if (isNaN(i) || i <= 0) return;
    const s = T(t);
    if (!s) return;
    const a = await o.setFontSize(i, s);
    n(a);
  },
  insertImage: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = document.createElement("input");
    e.type = "file", e.accept = "image/png,image/jpeg,image/gif,image/webp", e.style.cssText = "position:fixed;width:0;height:0;opacity:0;pointer-events:none", document.body.appendChild(e), e.addEventListener("change", async () => {
      var c;
      const i = (c = e.files) == null ? void 0 : c[0];
      if (document.body.removeChild(e), !i) return;
      const s = await new Promise((d, h) => {
        const u = new FileReader();
        u.onload = () => d(u.result.split(",")[1]), u.onerror = h, u.readAsDataURL(i);
      }), a = await new Promise((d) => {
        const h = new Image();
        h.onload = () => {
          d({ widthEmu: h.naturalWidth * 9525, heightEmu: h.naturalHeight * 9525 }), URL.revokeObjectURL(h.src);
        }, h.src = URL.createObjectURL(i);
      }), r = T(t);
      if (!r) return;
      const l = await o.insertImage(
        {
          imageData: s,
          contentMimeType: i.type,
          widthEmu: a.widthEmu,
          heightEmu: a.heightEmu,
          wrapMode: "Inline"
        },
        r
      );
      n(l), t.focus();
    }), e.click();
  },
  exportDocx: async ({ engine: o }) => {
    const t = await o.exportDocx(), n = new Blob([t], {
      type: "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    }), e = URL.createObjectURL(n), i = document.createElement("a");
    i.href = e, i.download = "document.docx", i.click(), URL.revokeObjectURL(e);
  },
  toggleGridLines: async ({ canvas: o }) => {
    const t = o.classList.toggle("show-grid");
    try {
      localStorage.setItem("documentEditor.gridLines", t ? "1" : "0");
    } catch {
    }
  },
  togglePilcrow: async ({ canvas: o }) => {
    const t = o.classList.toggle("show-pilcrow");
    try {
      localStorage.setItem("documentEditor.pilcrow", t ? "1" : "0");
    } catch {
    }
  },
  importDocx: async ({ engine: o, canvas: t, onResponse: n }) => {
    const e = document.createElement("input");
    e.type = "file", e.accept = ".docx", e.style.cssText = "position:fixed;width:0;height:0;opacity:0;pointer-events:none", document.body.appendChild(e), e.addEventListener("change", async () => {
      var l;
      const i = (l = e.files) == null ? void 0 : l[0];
      if (document.body.removeChild(e), !i) return;
      const s = await i.arrayBuffer(), a = new Uint8Array(s), r = await o.importDocx(a);
      n(r), t.focus();
    }), e.click();
  }
}, y = (o) => o, he = {
  id: "word",
  name: "Word",
  description: "Microsoft Word-style toolbar with two rows",
  theme: "word",
  rows: [
    // Row 1: main editing tools
    [
      {
        id: "history",
        items: [
          {
            id: "undo",
            type: "button",
            icon: y(ht),
            tooltip: "Undo",
            shortcut: "Ctrl+Z",
            action: "undo"
          },
          {
            id: "redo",
            type: "button",
            icon: y(gt),
            tooltip: "Redo",
            shortcut: "Ctrl+Y",
            action: "redo"
          }
        ]
      },
      {
        id: "fonts",
        items: [
          {
            id: "fontFamily",
            type: "combobox",
            tooltip: "Font Family",
            options: [
              "Calibri",
              "Arial",
              "Arimo",
              "Times New Roman",
              "Tinos",
              "Georgia",
              "Verdana",
              "Trebuchet MS",
              "Courier New",
              "Cousine",
              "Comic Sans MS",
              "Impact",
              "Palatino Linotype",
              "Tahoma",
              "Century Gothic",
              "Roboto",
              "Open Sans",
              "Lato"
            ].map((o) => ({ value: o, label: o })),
            getValue: (o) => o.fontFamily ?? "",
            action: "fontFamily",
            width: "150px",
            placeholder: "Font"
          },
          {
            id: "fontSize",
            type: "combobox",
            tooltip: "Font Size",
            options: [8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 32, 36, 40, 44, 48, 54, 60, 72].map((o) => ({ value: String(o), label: String(o) })),
            getValue: (o) => o.fontSize != null ? String(Math.round(o.fontSize)) : "",
            action: "fontSize",
            width: "55px",
            placeholder: "Size"
          }
        ]
      },
      {
        id: "style",
        items: [
          {
            id: "paragraphStyle",
            type: "select",
            tooltip: "Paragraph style",
            options: [
              { value: "Normal", label: "Normal" },
              { value: "Heading1", label: "Heading 1" },
              { value: "Heading2", label: "Heading 2" },
              { value: "Heading3", label: "Heading 3" },
              { value: "Heading4", label: "Heading 4" }
            ],
            getValue: (o) => o.paragraphStyle ?? "Normal",
            action: "paragraphStyle",
            width: "120px"
          }
        ]
      },
      {
        id: "format",
        items: [
          {
            id: "bold",
            type: "toggle",
            icon: y(pt),
            tooltip: "Bold",
            shortcut: "Ctrl+B",
            action: "bold",
            isActive: (o) => o.bold
          },
          {
            id: "italic",
            type: "toggle",
            icon: y(ut),
            tooltip: "Italic",
            shortcut: "Ctrl+I",
            action: "italic",
            isActive: (o) => o.italic
          },
          {
            id: "underline",
            type: "toggle",
            icon: y(mt),
            tooltip: "Underline",
            shortcut: "Ctrl+U",
            action: "underline",
            isActive: (o) => o.underline
          },
          {
            id: "strikethrough",
            type: "toggle",
            icon: y(Gt),
            tooltip: "Strikethrough",
            action: "strikethrough",
            isActive: (o) => o.strikethrough
          }
        ]
      },
      {
        id: "align",
        items: [
          {
            id: "alignLeft",
            type: "toggle",
            icon: y(ft),
            tooltip: "Align Left",
            action: "alignLeft",
            isActive: (o) => o.alignment === "left" || !o.alignment
          },
          {
            id: "alignCenter",
            type: "toggle",
            icon: y(yt),
            tooltip: "Center",
            action: "alignCenter",
            isActive: (o) => o.alignment === "center"
          },
          {
            id: "alignRight",
            type: "toggle",
            icon: y(bt),
            tooltip: "Align Right",
            action: "alignRight",
            isActive: (o) => o.alignment === "right"
          },
          {
            id: "alignJustify",
            type: "toggle",
            icon: y(Ut),
            tooltip: "Justify",
            action: "alignJustify",
            isActive: (o) => o.alignment === "both"
          }
        ]
      },
      {
        id: "lists",
        items: [
          {
            id: "bullet",
            type: "toggle",
            icon: y(vt),
            tooltip: "Bullet List",
            action: "bullet",
            isActive: (o) => o.listType === "bullet"
          },
          {
            id: "numbered",
            type: "toggle",
            icon: y(wt),
            tooltip: "Numbered List",
            action: "numbered",
            isActive: (o) => o.listType === "numbered"
          }
        ]
      },
      {
        id: "insert",
        items: [
          {
            id: "insertTable",
            type: "button",
            icon: y(Yt),
            tooltip: "Insert Table",
            action: "insertTable"
          },
          {
            id: "insertLink",
            type: "button",
            icon: y(Xt),
            tooltip: "Insert Hyperlink",
            action: "insertLink"
          },
          {
            id: "insertImage",
            type: "button",
            icon: y(xt),
            tooltip: "Insert Image",
            action: "insertImage"
          }
        ]
      },
      {
        id: "file",
        items: [
          {
            id: "exportDocx",
            type: "button",
            icon: y(qt),
            tooltip: "Export .docx",
            action: "exportDocx"
          },
          {
            id: "importDocx",
            type: "button",
            icon: y(Zt),
            tooltip: "Import .docx",
            action: "importDocx"
          }
        ]
      }
    ],
    // Row 2: cell tools + page layout
    [
      {
        id: "cellTools",
        items: [
          {
            id: "cellBorders",
            type: "dropdown",
            icon: y(Vt),
            tooltip: "Cell Borders",
            options: [
              { label: "No Borders", value: "none" },
              { label: "All Thin", value: "thin" },
              { label: "All Medium", value: "medium" },
              { label: "All Thick", value: "thick" }
            ],
            action: "cellBorders"
          },
          {
            id: "cellBackground",
            type: "button",
            icon: y(Kt),
            tooltip: "Cell Background Color",
            action: "cellBackground"
          },
          {
            id: "removeCellBackground",
            type: "button",
            icon: y(ve),
            tooltip: "Remove Cell Background",
            action: "removeCellBackground"
          }
        ]
      },
      {
        id: "pageLayout",
        items: [
          {
            id: "sectionBreak",
            type: "dropdown",
            icon: y(Jt),
            tooltip: "Insert Section Break",
            options: [
              { label: "Next Page", value: "nextPage" },
              { label: "Continuous", value: "continuous" },
              { label: "Even Page", value: "evenPage" },
              { label: "Odd Page", value: "oddPage" }
            ],
            action: "sectionBreak"
          },
          {
            id: "portrait",
            type: "button",
            icon: y(Qt),
            tooltip: "Portrait Orientation",
            action: "portrait"
          },
          {
            id: "landscape",
            type: "button",
            icon: y(te),
            tooltip: "Landscape Orientation",
            action: "landscape"
          },
          {
            id: "columns",
            type: "dropdown",
            icon: y(we),
            tooltip: "Columns",
            options: [
              { label: "1 Column", value: "1" },
              { label: "2 Columns", value: "2" },
              { label: "3 Columns", value: "3" }
            ],
            action: "columns"
          }
        ]
      },
      {
        id: "view",
        items: [
          {
            id: "toggleGridLines",
            type: "toggle",
            icon: y(ee),
            tooltip: "Grid Lines",
            action: "toggleGridLines",
            isActive: (o) => {
              var t;
              return !!((t = document.querySelector(".editor-canvas")) != null && t.classList.contains("show-grid"));
            }
          },
          {
            id: "togglePilcrow",
            type: "toggle",
            icon: y(ne),
            tooltip: "Show/Hide Paragraph Marks",
            action: "togglePilcrow",
            isActive: (o) => {
              var t;
              return !!((t = document.querySelector(".editor-canvas")) != null && t.classList.contains("show-pilcrow"));
            }
          }
        ]
      }
    ]
  ]
}, hn = {
  id: "gdocs",
  name: "Google Docs",
  description: "Google Docs-style single-row toolbar",
  theme: "gdocs",
  rows: [
    [
      {
        id: "file",
        items: [
          {
            id: "exportDocx",
            type: "button",
            icon: y(qt),
            tooltip: "Export .docx",
            action: "exportDocx"
          },
          {
            id: "importDocx",
            type: "button",
            icon: y(Zt),
            tooltip: "Import .docx",
            action: "importDocx"
          }
        ]
      },
      {
        id: "history",
        items: [
          {
            id: "undo",
            type: "button",
            icon: y(ht),
            tooltip: "Undo",
            shortcut: "Ctrl+Z",
            action: "undo"
          },
          {
            id: "redo",
            type: "button",
            icon: y(gt),
            tooltip: "Redo",
            shortcut: "Ctrl+Y",
            action: "redo"
          }
        ]
      },
      {
        id: "fonts",
        items: [
          {
            id: "fontFamily",
            type: "combobox",
            tooltip: "Font Family",
            options: [
              "Calibri",
              "Arial",
              "Arimo",
              "Times New Roman",
              "Tinos",
              "Georgia",
              "Verdana",
              "Trebuchet MS",
              "Courier New",
              "Cousine",
              "Comic Sans MS",
              "Impact",
              "Palatino Linotype",
              "Tahoma",
              "Century Gothic",
              "Roboto",
              "Open Sans",
              "Lato"
            ].map((o) => ({ value: o, label: o })),
            getValue: (o) => o.fontFamily ?? "",
            action: "fontFamily",
            width: "150px",
            placeholder: "Font"
          },
          {
            id: "fontSize",
            type: "combobox",
            tooltip: "Font Size",
            options: [8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 32, 36, 40, 44, 48, 54, 60, 72].map((o) => ({ value: String(o), label: String(o) })),
            getValue: (o) => o.fontSize != null ? String(Math.round(o.fontSize)) : "",
            action: "fontSize",
            width: "55px",
            placeholder: "Size"
          }
        ]
      },
      {
        id: "style",
        items: [
          {
            id: "paragraphStyle",
            type: "select",
            tooltip: "Paragraph style",
            options: [
              { value: "Normal", label: "Normal" },
              { value: "Heading1", label: "Heading 1" },
              { value: "Heading2", label: "Heading 2" },
              { value: "Heading3", label: "Heading 3" },
              { value: "Heading4", label: "Heading 4" }
            ],
            getValue: (o) => o.paragraphStyle ?? "Normal",
            action: "paragraphStyle",
            width: "110px"
          }
        ]
      },
      {
        id: "format",
        items: [
          {
            id: "bold",
            type: "toggle",
            icon: y(pt),
            tooltip: "Bold",
            shortcut: "Ctrl+B",
            action: "bold",
            isActive: (o) => o.bold
          },
          {
            id: "italic",
            type: "toggle",
            icon: y(ut),
            tooltip: "Italic",
            shortcut: "Ctrl+I",
            action: "italic",
            isActive: (o) => o.italic
          },
          {
            id: "underline",
            type: "toggle",
            icon: y(mt),
            tooltip: "Underline",
            shortcut: "Ctrl+U",
            action: "underline",
            isActive: (o) => o.underline
          },
          {
            id: "strikethrough",
            type: "toggle",
            icon: y(Gt),
            tooltip: "Strikethrough",
            action: "strikethrough",
            isActive: (o) => o.strikethrough
          }
        ]
      },
      {
        id: "align",
        items: [
          {
            id: "alignLeft",
            type: "toggle",
            icon: y(ft),
            tooltip: "Align Left",
            action: "alignLeft",
            isActive: (o) => o.alignment === "left" || !o.alignment
          },
          {
            id: "alignCenter",
            type: "toggle",
            icon: y(yt),
            tooltip: "Center",
            action: "alignCenter",
            isActive: (o) => o.alignment === "center"
          },
          {
            id: "alignRight",
            type: "toggle",
            icon: y(bt),
            tooltip: "Align Right",
            action: "alignRight",
            isActive: (o) => o.alignment === "right"
          },
          {
            id: "alignJustify",
            type: "toggle",
            icon: y(Ut),
            tooltip: "Justify",
            action: "alignJustify",
            isActive: (o) => o.alignment === "both"
          }
        ]
      },
      {
        id: "lists",
        items: [
          {
            id: "bullet",
            type: "toggle",
            icon: y(vt),
            tooltip: "Bullet List",
            action: "bullet",
            isActive: (o) => o.listType === "bullet"
          },
          {
            id: "numbered",
            type: "toggle",
            icon: y(wt),
            tooltip: "Numbered List",
            action: "numbered",
            isActive: (o) => o.listType === "numbered"
          }
        ]
      },
      {
        id: "insert",
        items: [
          {
            id: "insertTable",
            type: "button",
            icon: y(Yt),
            tooltip: "Insert Table",
            action: "insertTable"
          },
          {
            id: "insertLink",
            type: "button",
            icon: y(Xt),
            tooltip: "Insert Hyperlink",
            action: "insertLink"
          },
          {
            id: "insertImage",
            type: "button",
            icon: y(xt),
            tooltip: "Insert Image",
            action: "insertImage"
          }
        ]
      },
      {
        id: "cellTools",
        items: [
          {
            id: "cellBorders",
            type: "dropdown",
            icon: y(Vt),
            tooltip: "Cell Borders",
            options: [
              { label: "No Borders", value: "none" },
              { label: "All Thin", value: "thin" },
              { label: "All Medium", value: "medium" },
              { label: "All Thick", value: "thick" }
            ],
            action: "cellBorders"
          },
          {
            id: "cellBackground",
            type: "button",
            icon: y(Kt),
            tooltip: "Cell Background Color",
            action: "cellBackground"
          }
        ]
      },
      {
        id: "pageLayout",
        items: [
          {
            id: "sectionBreak",
            type: "dropdown",
            icon: y(Jt),
            tooltip: "Insert Section Break",
            options: [
              { label: "Next Page", value: "nextPage" },
              { label: "Continuous", value: "continuous" },
              { label: "Even Page", value: "evenPage" },
              { label: "Odd Page", value: "oddPage" }
            ],
            action: "sectionBreak"
          },
          {
            id: "portrait",
            type: "button",
            icon: y(Qt),
            tooltip: "Portrait Orientation",
            action: "portrait"
          },
          {
            id: "landscape",
            type: "button",
            icon: y(te),
            tooltip: "Landscape Orientation",
            action: "landscape"
          }
        ]
      },
      {
        id: "view",
        items: [
          {
            id: "toggleGridLines",
            type: "toggle",
            icon: y(ee),
            tooltip: "Grid Lines",
            action: "toggleGridLines",
            isActive: (o) => {
              var t;
              return !!((t = document.querySelector(".editor-canvas")) != null && t.classList.contains("show-grid"));
            }
          },
          {
            id: "togglePilcrow",
            type: "toggle",
            icon: y(ne),
            tooltip: "Show/Hide Paragraph Marks",
            action: "togglePilcrow",
            isActive: (o) => {
              var t;
              return !!((t = document.querySelector(".editor-canvas")) != null && t.classList.contains("show-pilcrow"));
            }
          }
        ]
      }
    ]
  ]
}, gn = {
  id: "compact",
  name: "Compact",
  description: "Minimal single-row toolbar",
  theme: "compact",
  rows: [
    [
      {
        id: "history",
        items: [
          {
            id: "undo",
            type: "button",
            icon: y(ht),
            tooltip: "Undo",
            shortcut: "Ctrl+Z",
            action: "undo"
          },
          {
            id: "redo",
            type: "button",
            icon: y(gt),
            tooltip: "Redo",
            shortcut: "Ctrl+Y",
            action: "redo"
          }
        ]
      },
      {
        id: "format",
        items: [
          {
            id: "bold",
            type: "toggle",
            icon: y(pt),
            tooltip: "Bold",
            shortcut: "Ctrl+B",
            action: "bold",
            isActive: (o) => o.bold
          },
          {
            id: "italic",
            type: "toggle",
            icon: y(ut),
            tooltip: "Italic",
            shortcut: "Ctrl+I",
            action: "italic",
            isActive: (o) => o.italic
          },
          {
            id: "underline",
            type: "toggle",
            icon: y(mt),
            tooltip: "Underline",
            shortcut: "Ctrl+U",
            action: "underline",
            isActive: (o) => o.underline
          }
        ]
      },
      {
        id: "align",
        items: [
          {
            id: "alignLeft",
            type: "toggle",
            icon: y(ft),
            tooltip: "Align Left",
            action: "alignLeft",
            isActive: (o) => o.alignment === "left" || !o.alignment
          },
          {
            id: "alignCenter",
            type: "toggle",
            icon: y(yt),
            tooltip: "Center",
            action: "alignCenter",
            isActive: (o) => o.alignment === "center"
          },
          {
            id: "alignRight",
            type: "toggle",
            icon: y(bt),
            tooltip: "Align Right",
            action: "alignRight",
            isActive: (o) => o.alignment === "right"
          }
        ]
      },
      {
        id: "lists",
        items: [
          {
            id: "bullet",
            type: "toggle",
            icon: y(vt),
            tooltip: "Bullet List",
            action: "bullet",
            isActive: (o) => o.listType === "bullet"
          },
          {
            id: "numbered",
            type: "toggle",
            icon: y(wt),
            tooltip: "Numbered List",
            action: "numbered",
            isActive: (o) => o.listType === "numbered"
          }
        ]
      },
      {
        id: "insert",
        items: [
          {
            id: "insertImage",
            type: "button",
            icon: y(xt),
            tooltip: "Insert Image",
            action: "insertImage"
          }
        ]
      }
    ]
  ]
}, pn = "flex items-center justify-center w-7 h-7 rounded text-gray-700 transition-colors hover:bg-gray-100 active:bg-gray-200 disabled:opacity-40 disabled:cursor-not-allowed", un = "flex items-center justify-center w-7 h-7 rounded text-blue-700 bg-blue-100 transition-colors hover:bg-blue-200 disabled:opacity-40 disabled:cursor-not-allowed", mn = "flex items-center justify-center w-7 h-7 rounded-sm text-gray-700 transition-colors hover:bg-gray-200 active:bg-gray-300 disabled:opacity-40 disabled:cursor-not-allowed", fn = "flex items-center justify-center w-7 h-7 rounded-sm text-blue-600 bg-blue-50 transition-colors hover:bg-blue-100 disabled:opacity-40 disabled:cursor-not-allowed", yn = "flex items-center justify-center w-6 h-6 rounded text-gray-600 transition-colors hover:bg-gray-100 disabled:opacity-40 disabled:cursor-not-allowed", bn = "flex items-center justify-center w-6 h-6 rounded text-blue-700 bg-blue-100 transition-colors hover:bg-blue-200 disabled:opacity-40 disabled:cursor-not-allowed", vn = "flex items-center gap-0.5 h-7 px-1.5 rounded text-gray-700 text-xs transition-colors hover:bg-gray-100 active:bg-gray-200 disabled:opacity-40", wn = "flex items-center gap-0.5 h-7 px-1.5 rounded-sm text-gray-700 text-xs transition-colors hover:bg-gray-200 active:bg-gray-300 disabled:opacity-40", xn = "flex items-center gap-0.5 h-6 px-1 rounded text-gray-600 text-xs transition-colors hover:bg-gray-100 disabled:opacity-40";
function _t(o, t = 16) {
  const n = "http://www.w3.org/2000/svg", e = document.createElementNS(n, "svg");
  e.setAttribute("width", String(t)), e.setAttribute("height", String(t)), e.setAttribute("viewBox", "0 0 24 24"), e.setAttribute("fill", "none"), e.setAttribute("stroke", "currentColor"), e.setAttribute("stroke-width", "1.75"), e.setAttribute("stroke-linecap", "round"), e.setAttribute("stroke-linejoin", "round"), e.style.pointerEvents = "none", e.style.flexShrink = "0";
  for (const [i, s] of o) {
    const a = document.createElementNS(n, i);
    for (const [r, l] of Object.entries(s))
      a.setAttribute(r, l);
    e.appendChild(a);
  }
  return e;
}
function Cn() {
  const o = "http://www.w3.org/2000/svg", t = document.createElementNS(o, "svg");
  t.setAttribute("width", "10"), t.setAttribute("height", "10"), t.setAttribute("viewBox", "0 0 24 24"), t.setAttribute("fill", "none"), t.setAttribute("stroke", "currentColor"), t.setAttribute("stroke-width", "2.5"), t.setAttribute("stroke-linecap", "round"), t.setAttribute("stroke-linejoin", "round"), t.style.pointerEvents = "none";
  const n = document.createElementNS(o, "polyline");
  return n.setAttribute("points", "6 9 12 15 18 9"), t.appendChild(n), t;
}
class Sn {
  constructor(t, n, e, i, s = he) {
    g(this, "el");
    g(this, "ctx");
    g(this, "currentPreset");
    // Item element references by item ID
    g(this, "itemElements", /* @__PURE__ */ new Map());
    // Updaters for undo/redo and isEnabled state — receive full EngineResponse
    g(this, "stateUpdaters", []);
    // Updaters for format-sensitive controls (toggles, selects, combos) — receive FormatState only
    g(this, "formatStateUpdaters", []);
    this.ctx = { engine: n, canvas: e, onResponse: i }, this.currentPreset = s, this.el = document.createElement("div"), this.el.className = "bg-white border-b border-gray-200 flex-shrink-0 sticky top-0 z-50", t.appendChild(this.el), this.renderPreset(s), this.loadCustomization();
  }
  getElement() {
    return this.el;
  }
  updateState(t) {
    for (const n of this.stateUpdaters)
      n(t);
    for (const n of this.formatStateUpdaters)
      n(t.formatState);
  }
  updateFormatState(t) {
    for (const n of this.formatStateUpdaters)
      n(t);
  }
  switchPreset(t) {
    this.currentPreset = t, this.itemElements.clear(), this.stateUpdaters = [], this.formatStateUpdaters = [], this.el.innerHTML = "", this.renderPreset(t), this.loadCustomization();
  }
  setItemVisible(t, n) {
    const e = this.itemElements.get(t);
    e && (e.style.display = n ? "" : "none");
  }
  getHiddenItems() {
    const t = [];
    for (const [n, e] of this.itemElements)
      e.style.display === "none" && t.push(n);
    return t;
  }
  saveCustomization() {
    const t = {
      hiddenItems: this.getHiddenItems(),
      activePresetId: this.currentPreset.id
    };
    localStorage.setItem("documentEditor.toolbarConfig", JSON.stringify(t));
  }
  loadCustomization() {
    try {
      const t = localStorage.getItem("documentEditor.toolbarConfig");
      if (!t) return;
      const n = JSON.parse(t);
      for (const e of n.hiddenItems ?? [])
        this.setItemVisible(e, !1);
    } catch {
    }
  }
  // ── Private: rendering ───────────────────────────────────────────────────
  renderPreset(t) {
    for (const n of t.rows) {
      const e = this.buildRow(n, t.theme);
      this.el.appendChild(e);
    }
  }
  buildRow(t, n) {
    const e = document.createElement("div");
    e.className = "flex items-center flex-wrap px-2 py-1 gap-0.5";
    for (let i = 0; i < t.length; i++) {
      if (i > 0) {
        const s = document.createElement("div");
        s.className = "w-px h-5 bg-gray-200 mx-1 flex-shrink-0", e.appendChild(s);
      }
      e.appendChild(this.buildGroup(t[i], n));
    }
    return e;
  }
  buildGroup(t, n) {
    const e = document.createElement("div");
    e.className = "flex items-center gap-0.5";
    for (const i of t.items) {
      const s = this.buildItem(i, n);
      s && (e.appendChild(s), i.type !== "separator" && this.itemElements.set(i.id, s));
    }
    return e;
  }
  buildItem(t, n) {
    switch (t.type) {
      case "button":
      case "toggle":
        return this.buildButton(t, n);
      case "select":
        return this.buildSelect(t, n);
      case "dropdown":
        return this.buildDropdown(t, n);
      case "combobox":
        return this.buildCombo(t, n);
      case "separator":
        return this.buildInlineSeparator();
      default:
        return null;
    }
  }
  buildButton(t, n) {
    const e = document.createElement("button");
    e.type = "button", e.title = t.shortcut ? `${t.tooltip} (${t.shortcut})` : t.tooltip;
    const i = this.btnBase(n), s = this.btnActive(n);
    if (e.className = i, e.appendChild(_t(t.icon, n === "compact" ? 14 : 16)), e.addEventListener("mousedown", (a) => a.preventDefault()), e.addEventListener("click", async () => {
      var a;
      await ((a = D[t.action]) == null ? void 0 : a.call(D, this.ctx)), this.ctx.canvas.focus();
    }), t.type === "toggle" && t.isActive) {
      const a = t.isActive;
      this.formatStateUpdaters.push((r) => {
        e.className = a(r) ? s : i;
      });
    }
    if (t.id === "undo" && this.stateUpdaters.push((a) => {
      e.disabled = !a.canUndo;
    }), t.id === "redo" && this.stateUpdaters.push((a) => {
      e.disabled = !a.canRedo;
    }), t.isEnabled) {
      const a = t.isEnabled;
      this.stateUpdaters.push((r) => {
        e.disabled = !a(r.formatState);
      });
    }
    return e;
  }
  buildSelect(t, n) {
    const e = document.createElement("select");
    e.title = t.tooltip, e.className = "h-7 px-2 text-xs border border-gray-300 rounded bg-white text-gray-700 cursor-pointer focus:outline-none focus:ring-1 focus:ring-blue-400 hover:border-gray-400", t.width && (e.style.width = t.width);
    for (const s of t.options) {
      const a = document.createElement("option");
      a.value = s.value, a.textContent = s.label, e.appendChild(a);
    }
    e.addEventListener("change", async () => {
      var s;
      await ((s = D[t.action]) == null ? void 0 : s.call(D, this.ctx, e.value)), this.ctx.canvas.focus();
    });
    const i = t.getValue;
    return this.formatStateUpdaters.push((s) => {
      e.value = i(s);
    }), e;
  }
  buildDropdown(t, n) {
    const e = document.createElement("div");
    e.className = "relative";
    const i = document.createElement("button");
    i.type = "button", i.title = t.tooltip, i.className = this.dropBase(n), i.appendChild(_t(t.icon, n === "compact" ? 13 : 15)), i.appendChild(Cn());
    const s = document.createElement("div");
    s.className = "absolute top-full left-0 z-50 min-w-36 bg-white border border-gray-200 rounded shadow-lg py-1 hidden";
    for (const a of t.options) {
      const r = document.createElement("button");
      r.type = "button", r.className = "block w-full text-left px-3 py-1.5 text-xs text-gray-700 hover:bg-gray-100 whitespace-nowrap", r.textContent = a.label, r.addEventListener("mousedown", (l) => l.preventDefault()), r.addEventListener("click", async () => {
        var l;
        s.classList.add("hidden"), await ((l = D[t.action]) == null ? void 0 : l.call(D, this.ctx, a.value)), this.ctx.canvas.focus();
      }), s.appendChild(r);
    }
    return i.addEventListener("mousedown", (a) => a.preventDefault()), i.addEventListener("click", (a) => {
      a.stopPropagation(), s.classList.toggle("hidden");
    }), document.addEventListener("click", () => s.classList.add("hidden")), e.appendChild(i), e.appendChild(s), e;
  }
  buildCombo(t, n) {
    const e = document.createElement("span");
    e.className = "relative inline-flex items-center";
    const i = `wave-combo-${t.id}`, s = document.createElement("input"), a = document.createElement("datalist");
    a.id = i, s.setAttribute("list", i), s.type = "text", s.title = t.tooltip, t.placeholder && (s.placeholder = t.placeholder), t.width && (s.style.width = t.width), s.className = "h-7 px-2 text-xs border border-gray-300 rounded bg-white text-gray-700 focus:outline-none focus:ring-1 focus:ring-blue-400 hover:border-gray-400";
    for (const c of t.options) {
      const d = document.createElement("option");
      d.value = c.value, a.appendChild(d);
    }
    let r = null;
    s.addEventListener("mousedown", () => {
      r = T(this.ctx.canvas);
    });
    const l = async () => {
      var d;
      const c = r ?? T(this.ctx.canvas);
      if (c && s.value.trim()) {
        const h = t.action;
        let u;
        if (h === "fontFamily")
          u = await this.ctx.engine.setFontFamily(s.value.trim(), c);
        else if (h === "fontSize") {
          const p = parseFloat(s.value);
          !isNaN(p) && p > 0 && (u = await this.ctx.engine.setFontSize(p, c));
        } else
          await ((d = D[t.action]) == null ? void 0 : d.call(D, this.ctx, s.value));
        u && this.ctx.onResponse(u);
      }
      this.ctx.canvas.focus();
    };
    return s.addEventListener("change", l), s.addEventListener("keydown", (c) => {
      c.key === "Enter" && (c.preventDefault(), l()), c.key === "Escape" && this.ctx.canvas.focus();
    }), this.formatStateUpdaters.push((c) => {
      const d = t.getValue(c);
      document.activeElement !== s && (s.value = d);
    }), e.appendChild(s), e.appendChild(a), e;
  }
  buildInlineSeparator() {
    const t = document.createElement("div");
    return t.className = "w-px h-4 bg-gray-200 mx-0.5 flex-shrink-0", t;
  }
  // ── Theme helpers ────────────────────────────────────────────────────────
  btnBase(t) {
    return t === "gdocs" ? mn : t === "compact" ? yn : pn;
  }
  btnActive(t) {
    return t === "gdocs" ? fn : t === "compact" ? bn : un;
  }
  dropBase(t) {
    return t === "gdocs" ? wn : t === "compact" ? xn : vn;
  }
}
function at(o, t = 14) {
  const n = "http://www.w3.org/2000/svg", e = document.createElementNS(n, "svg");
  e.setAttribute("width", String(t)), e.setAttribute("height", String(t)), e.setAttribute("viewBox", "0 0 24 24"), e.setAttribute("fill", "none"), e.setAttribute("stroke", "currentColor"), e.setAttribute("stroke-width", "1.75"), e.setAttribute("stroke-linecap", "round"), e.setAttribute("stroke-linejoin", "round"), e.style.pointerEvents = "none", e.style.flexShrink = "0";
  for (const [i, s] of o) {
    const a = document.createElementNS(n, i);
    for (const [r, l] of Object.entries(s))
      a.setAttribute(r, l);
    e.appendChild(a);
  }
  return e;
}
const En = [
  { id: "outline", label: "Outline", icon: xe },
  { id: "stats", label: "Stats", icon: Ce },
  { id: "xml", label: "XML", icon: Se }
];
class kn {
  constructor(t) {
    g(this, "el");
    g(this, "tabsRow");
    g(this, "panelContainer");
    g(this, "outlinePanel");
    g(this, "statsPanel");
    g(this, "xmlPanel");
    g(this, "toggleBtn");
    g(this, "collapsed", !1);
    g(this, "tabButtons", /* @__PURE__ */ new Map());
    this.el = document.createElement("div"), this.el.className = "flex flex-col border-l border-gray-200 bg-white flex-shrink-0 overflow-hidden transition-[width] duration-200", this.el.style.width = "240px";
    const n = document.createElement("div");
    n.className = "flex items-center justify-end px-1 py-1 border-b border-gray-100", this.toggleBtn = document.createElement("button"), this.toggleBtn.type = "button", this.toggleBtn.title = "Toggle sidebar", this.toggleBtn.className = "flex items-center justify-center w-6 h-6 rounded text-gray-400 hover:bg-gray-100 hover:text-gray-600 transition-colors", this.toggleBtn.appendChild(at(Ht, 14)), this.toggleBtn.addEventListener("click", () => this.toggle()), n.appendChild(this.toggleBtn), this.el.appendChild(n), this.tabsRow = document.createElement("div"), this.tabsRow.className = "flex border-b border-gray-200 bg-gray-50";
    for (const e of En) {
      const i = this.createTabBtn(e.id, e.label, e.icon);
      this.tabsRow.appendChild(i), this.tabButtons.set(e.id, i);
    }
    this.el.appendChild(this.tabsRow), this.panelContainer = document.createElement("div"), this.panelContainer.className = "flex-1 overflow-y-auto", this.outlinePanel = this.createPanel(), this.statsPanel = this.createPanel(), this.xmlPanel = this.createPanel(), this.panelContainer.append(this.outlinePanel, this.statsPanel, this.xmlPanel), this.el.appendChild(this.panelContainer), t.appendChild(this.el), this.activateTab("outline");
  }
  getElement() {
    return this.el;
  }
  toggle() {
    this.collapsed = !this.collapsed, this.el.style.width = this.collapsed ? "40px" : "240px", this.tabsRow.style.display = this.collapsed ? "none" : "", this.panelContainer.style.display = this.collapsed ? "none" : "", this.toggleBtn.innerHTML = "";
    const t = this.collapsed ? Ee : Ht;
    this.toggleBtn.appendChild(at(t, 14));
  }
  updateOutline(t) {
    this.outlinePanel.innerHTML = "";
    const n = t.filter(
      (s) => ["h1", "h2", "h3", "h4"].includes(s.tag)
    );
    if (n.length === 0) {
      const s = document.createElement("p");
      s.className = "text-xs text-gray-400 text-center py-4", s.textContent = "No headings found.", this.outlinePanel.appendChild(s);
      return;
    }
    const e = document.createElement("ul");
    e.className = "space-y-0.5";
    const i = {
      h1: "pl-2",
      h2: "pl-5",
      h3: "pl-8",
      h4: "pl-11"
    };
    for (const s of n) {
      const a = document.createElement("li");
      a.className = `flex items-center gap-1 py-0.5 text-xs text-gray-700 hover:text-blue-600 cursor-pointer rounded px-1 hover:bg-gray-50 ${i[s.tag] ?? "pl-2"}`, a.textContent = ct(s), a.dataset.nodeId = s.id, s.tag === "h1" && a.classList.add("font-medium"), (s.tag === "h3" || s.tag === "h4") && a.classList.add("text-gray-500"), a.addEventListener("click", () => {
        const r = document.querySelector(`[data-node-id="${s.id}"]`);
        r == null || r.scrollIntoView({ behavior: "smooth", block: "center" });
      }), e.appendChild(a);
    }
    this.outlinePanel.appendChild(e);
  }
  updateStats(t) {
    const n = t.map(ct).join(" "), e = n.trim() ? n.trim().split(/\s+/).length : 0, i = n.length, s = t.filter(
      (c) => ["p", "h1", "h2", "h3", "h4"].includes(c.tag)
    ).length, a = Math.max(1, Math.ceil(e / 200));
    this.statsPanel.innerHTML = "";
    const r = document.createElement("div");
    r.className = "grid grid-cols-2 gap-2";
    const l = [
      { value: String(e), label: "Words" },
      { value: String(i), label: "Characters" },
      { value: String(s), label: "Paragraphs" },
      { value: `${a} min`, label: "Reading time" }
    ];
    for (const c of l) {
      const d = document.createElement("div");
      d.className = "text-center p-2 bg-gray-50 rounded";
      const h = document.createElement("span");
      h.className = "block text-lg font-bold text-gray-800", h.textContent = c.value;
      const u = document.createElement("span");
      u.className = "block text-xs text-gray-400 mt-0.5", u.textContent = c.label, d.append(h, u), r.appendChild(d);
    }
    this.statsPanel.appendChild(r);
  }
  updateXmlDebug(t) {
    this.xmlPanel.innerHTML = "";
    const n = document.createElement("pre");
    n.className = "text-xs leading-relaxed whitespace-pre-wrap break-all font-mono bg-gray-50 rounded p-2 max-h-full overflow-auto text-gray-600", n.textContent = t, this.xmlPanel.appendChild(n);
  }
  // ── Private helpers ──────────────────────────────────────────────────────
  createTabBtn(t, n, e) {
    const i = document.createElement("button");
    i.type = "button", i.title = n, i.dataset.panel = t, i.className = this.tabInactiveClass(), i.appendChild(at(e, 13));
    const s = document.createElement("span");
    return s.textContent = n, i.appendChild(s), i.addEventListener("click", () => this.activateTab(t)), i;
  }
  createPanel() {
    const t = document.createElement("div");
    return t.className = "hidden p-3", t;
  }
  activateTab(t) {
    for (const [i, s] of this.tabButtons)
      s.className = i === t ? this.tabActiveClass() : this.tabInactiveClass();
    const n = [this.outlinePanel, this.statsPanel, this.xmlPanel], e = ["outline", "stats", "xml"];
    for (let i = 0; i < n.length; i++)
      n[i].className = e[i] === t ? "block p-3" : "hidden p-3";
  }
  tabActiveClass() {
    return "flex items-center justify-center gap-1 flex-1 px-2 py-2 text-xs font-medium text-blue-600 border-b-2 border-blue-500 bg-white transition-colors";
  }
  tabInactiveClass() {
    return "flex items-center justify-center gap-1 flex-1 px-2 py-2 text-xs font-medium text-gray-500 border-b-2 border-transparent hover:text-gray-700 hover:border-gray-300 bg-gray-50 transition-colors";
  }
}
function ct(o) {
  return o.text !== void 0 && o.text !== null ? o.text : (o.children ?? []).map(ct).join("");
}
class Tn {
  constructor(t) {
    g(this, "el");
    g(this, "pageInfo");
    g(this, "wordCount");
    g(this, "zoomLabel");
    g(this, "currentPage", 1);
    g(this, "totalPages", 1);
    g(this, "words", 0);
    this.el = document.createElement("div"), this.el.className = "flex items-center justify-between px-4 py-1.5 text-xs text-gray-500 bg-gray-50 border-t border-gray-200 select-none flex-shrink-0";
    const n = document.createElement("div");
    n.className = "flex items-center gap-3";
    const e = document.createElement("span");
    e.className = "text-blue-600 font-medium", e.textContent = "Editing", this.pageInfo = document.createElement("span"), this.pageInfo.className = "text-gray-500", this.wordCount = document.createElement("span"), this.wordCount.className = "text-gray-500", n.append(e, this.pageInfo, this.wordCount);
    const i = document.createElement("div");
    i.className = "flex items-center gap-1";
    const s = document.createElement("button");
    s.type = "button", s.textContent = "−", s.className = "w-5 h-5 flex items-center justify-center rounded hover:bg-gray-200 text-gray-600 text-sm leading-none transition-colors", s.title = "Zoom out", this.zoomLabel = document.createElement("span"), this.zoomLabel.className = "w-10 text-center text-gray-600 tabular-nums", this.zoomLabel.textContent = "100%";
    const a = document.createElement("button");
    a.type = "button", a.textContent = "+", a.className = "w-5 h-5 flex items-center justify-center rounded hover:bg-gray-200 text-gray-600 text-sm leading-none transition-colors", a.title = "Zoom in", s.addEventListener("click", () => {
      window.dispatchEvent(new CustomEvent("doc:zoom-out"));
    }), a.addEventListener("click", () => {
      window.dispatchEvent(new CustomEvent("doc:zoom-in"));
    }), i.append(s, this.zoomLabel, a), this.el.append(n, i), t.appendChild(this.el), this.refreshLeft();
  }
  getElement() {
    return this.el;
  }
  updatePageInfo(t, n) {
    this.currentPage = t, this.totalPages = n, this.refreshLeft();
  }
  updateWordCount(t) {
    const n = t.map(ge).join(" ");
    this.words = n.trim() ? n.trim().split(/\s+/).length : 0, this.refreshLeft();
  }
  updateZoom(t) {
    this.zoomLabel.textContent = `${Math.round(t)}%`;
  }
  refreshLeft() {
    this.pageInfo.textContent = `Page ${this.currentPage} of ${this.totalPages}`;
    const t = `${this.words} word${this.words !== 1 ? "s" : ""}`;
    this.wordCount.textContent = t;
  }
}
function ge(o) {
  return o.text !== void 0 && o.text !== null ? o.text : (o.children ?? []).map(ge).join("");
}
class Ln {
  constructor(t, n, e, i) {
    g(this, "el");
    g(this, "svg");
    g(this, "engine");
    g(this, "canvas");
    g(this, "onResponse");
    // Page dimensions in px
    g(this, "pageWidth", 816);
    g(this, "marginLeft", 96);
    g(this, "marginRight", 96);
    g(this, "zoom", 1);
    // Current indent values in twips
    g(this, "indentLeft", 0);
    g(this, "indentFirstLine", 0);
    this.engine = n, this.canvas = e, this.onResponse = i, this.el = document.createElement("div"), this.el.className = "ruler-h flex justify-center border-b border-gray-200 bg-gray-50 overflow-hidden flex-shrink-0 select-none";
    const s = "http://www.w3.org/2000/svg";
    this.svg = document.createElementNS(s, "svg"), this.svg.setAttribute("width", String(this.pageWidth)), this.svg.setAttribute("height", "24"), this.svg.classList.add("ruler-svg"), this.el.appendChild(this.svg), t.appendChild(this.el), this.render();
  }
  getElement() {
    return this.el;
  }
  setZoom(t) {
    this.zoom !== t && (this.zoom = t, this.svg.setAttribute("width", String(this.pageWidth * t)), this.render());
  }
  syncScrollLeft(t) {
    this.svg.style.transform = `translateX(-${t}px)`;
  }
  updateDimensions(t, n, e) {
    this.pageWidth = t, this.marginLeft = n, this.marginRight = e, this.svg.setAttribute("width", String(t * this.zoom)), this.render();
  }
  updateIndents(t, n) {
    this.indentLeft = t, this.indentFirstLine = n, this.render();
  }
  render() {
    const t = "http://www.w3.org/2000/svg";
    this.svg.innerHTML = "";
    const n = this.zoom, e = this.marginLeft * n, i = (this.pageWidth - this.marginRight) * n, s = document.createElementNS(t, "rect");
    s.setAttribute("x", "0"), s.setAttribute("y", "0"), s.setAttribute("width", String(this.pageWidth * n)), s.setAttribute("height", "24"), s.setAttribute("fill", "#e0e0e0"), this.svg.appendChild(s);
    const a = document.createElementNS(t, "rect");
    a.setAttribute("x", String(e)), a.setAttribute("y", "0"), a.setAttribute("width", String(i - e)), a.setAttribute("height", "24"), a.setAttribute("fill", "#fff"), this.svg.appendChild(a);
    const r = 96, l = this.pageWidth / r;
    for (let u = 0; u <= l; u++) {
      const p = u * r * n;
      if (this.addLine(p, 0, p, 14, "#666", 1), u > 0 && u < l) {
        const f = document.createElementNS(t, "text");
        f.setAttribute("x", String(p)), f.setAttribute("y", "22"), f.setAttribute("text-anchor", "middle"), f.setAttribute("font-size", "9"), f.setAttribute("fill", "#666"), f.textContent = String(u), this.svg.appendChild(f);
      }
      u < l && (this.addLine(p + r * n / 2, 4, p + r * n / 2, 14, "#999", 0.5), this.addLine(p + r * n / 4, 8, p + r * n / 4, 14, "#bbb", 0.5), this.addLine(p + 3 * r * n / 4, 8, p + 3 * r * n / 4, 14, "#bbb", 0.5));
    }
    const c = (u) => u * 96 / 1440, d = e + c(this.indentLeft + this.indentFirstLine) * n;
    this.addTriangle(d, 0, "down", "#4285f4", "first-line-indent");
    const h = e + c(this.indentLeft) * n;
    this.addTriangle(h, 18, "up", "#4285f4", "left-indent"), this.addTriangle(i, 18, "up", "#4285f4", "right-margin");
  }
  addLine(t, n, e, i, s, a) {
    const l = document.createElementNS("http://www.w3.org/2000/svg", "line");
    l.setAttribute("x1", String(t)), l.setAttribute("y1", String(n)), l.setAttribute("x2", String(e)), l.setAttribute("y2", String(i)), l.setAttribute("stroke", s), l.setAttribute("stroke-width", String(a)), this.svg.appendChild(l);
  }
  addTriangle(t, n, e, i, s) {
    const r = document.createElementNS("http://www.w3.org/2000/svg", "polygon"), l = 6;
    let c;
    e === "down" ? c = `${t - l},${n} ${t + l},${n} ${t},${n + l}` : c = `${t - l},${n + l} ${t + l},${n + l} ${t},${n}`, r.setAttribute("points", c), r.setAttribute("fill", i), r.style.cursor = "ew-resize";
    let d = 0, h = t;
    const u = (f) => {
      const b = f.clientX - d, w = h + b;
      let k;
      e === "down" ? k = `${w - l},${n} ${w + l},${n} ${w},${n + l}` : k = `${w - l},${n + l} ${w + l},${n + l} ${w},${n}`, r.setAttribute("points", k);
    }, p = async (f) => {
      document.removeEventListener("mousemove", u), document.removeEventListener("mouseup", p);
      const b = f.clientX - d, w = Math.round(b * 1440 / 96 / this.zoom), k = T(this.canvas);
      if (!k) return;
      const m = await this.engine.setIndent(w, 0, k);
      this.onResponse(m);
    };
    r.addEventListener("mousedown", (f) => {
      f.preventDefault(), d = f.clientX, h = t, document.addEventListener("mousemove", u), document.addEventListener("mouseup", p);
    }), this.svg.appendChild(r);
  }
}
class Pn {
  // matches pages.css .pages-wrapper padding
  constructor(t) {
    g(this, "el");
    g(this, "svg");
    g(this, "pageHeight", 1056);
    g(this, "marginTop", 96);
    g(this, "marginBottom", 96);
    g(this, "pageCount", 1);
    g(this, "zoom", 1);
    g(this, "activePage", 1);
    g(this, "activePageTopScrollY", 20);
    // scroll-y of active page's top (default = page 1)
    g(this, "totalScrollHeight", 0);
    // 0 = use formula fallback
    g(this, "gapHeight", 24);
    // matches GAP_HEIGHT in page-layout.ts
    g(this, "topPadding", 20);
    this.el = document.createElement("div"), this.el.className = "ruler-v w-6 border-r border-gray-200 bg-gray-50 overflow-hidden flex-shrink-0 select-none";
    const n = "http://www.w3.org/2000/svg";
    this.svg = document.createElementNS(n, "svg"), this.svg.setAttribute("width", "24"), this.svg.classList.add("ruler-svg"), this.el.appendChild(this.svg), t.appendChild(this.el), this.updateSvgHeight(), this.render();
  }
  getElement() {
    return this.el;
  }
  updateDimensions(t, n, e, i, s) {
    this.pageHeight = t, this.marginTop = n, this.marginBottom = e, i !== void 0 && (this.activePage = i), s !== void 0 && (this.activePageTopScrollY = s), this.updateSvgHeight(), this.render();
  }
  setTotalScrollHeight(t) {
    this.totalScrollHeight = t, this.updateSvgHeight(), this.render();
  }
  setPageCount(t) {
    this.pageCount !== t && (this.pageCount = t, this.updateSvgHeight(), this.render());
  }
  setZoom(t) {
    this.zoom !== t && (this.zoom = t, this.updateSvgHeight(), this.render());
  }
  /**
   * Sync vertical scroll position with the page container.
   * Uses CSS transform so the ruler follows at any scroll depth,
   * not limited by SVG content height.
   */
  syncScroll(t) {
    this.svg.style.transform = `translateY(-${t}px)`;
  }
  updateSvgHeight() {
    const t = this.totalScrollHeight > 0 ? this.totalScrollHeight * this.zoom : (this.topPadding + this.pageHeight * this.pageCount + this.gapHeight * (this.pageCount - 1) + this.topPadding) * this.zoom;
    this.svg.setAttribute("height", String(t));
  }
  render() {
    const t = "http://www.w3.org/2000/svg", e = this.zoom;
    this.svg.innerHTML = "";
    const i = this.totalScrollHeight > 0 ? this.totalScrollHeight * e : (this.topPadding + this.pageHeight * this.pageCount + this.gapHeight * (this.pageCount - 1) + this.topPadding) * e, s = document.createElementNS(t, "rect");
    s.setAttribute("x", "0"), s.setAttribute("y", "0"), s.setAttribute("width", "24"), s.setAttribute("height", String(i)), s.setAttribute("fill", "#e0e0e0"), this.svg.appendChild(s);
    const a = this.activePage - 1;
    if (a >= 0 && a < this.pageCount) {
      const r = this.activePageTopScrollY * e, l = r + this.marginTop * e, c = r + (this.pageHeight - this.marginBottom) * e, d = document.createElementNS(t, "rect");
      d.setAttribute("x", "0"), d.setAttribute("y", String(l)), d.setAttribute("width", "24"), d.setAttribute("height", String(c - l)), d.setAttribute("fill", "#fff"), this.svg.appendChild(d);
      const h = this.pageHeight / 96, u = this.marginTop / 96;
      for (let p = 0; p <= h; p++) {
        const f = r + p * 96 * e;
        if (this.addLine(10, f, 24, f, "#666", 1), p < h) {
          const b = p - u, w = Math.abs(b) < 0.01, k = b < -0.01, m = document.createElementNS(t, "text");
          m.setAttribute("x", "5"), m.setAttribute("y", String(f + 3)), m.setAttribute("text-anchor", "middle"), m.setAttribute("font-size", "9"), m.setAttribute("fill", w || k ? "#999" : "#666"), m.textContent = w ? "0" : String(Math.round(Math.abs(b))), this.svg.appendChild(m), this.addLine(14, f + 96 * e / 2, 24, f + 96 * e / 2, "#999", 0.5), this.addLine(18, f + 96 * e / 4, 24, f + 96 * e / 4, "#bbb", 0.5), this.addLine(18, f + 288 * e / 4, 24, f + 288 * e / 4, "#bbb", 0.5);
        }
      }
    }
  }
  addLine(t, n, e, i, s, a) {
    const l = document.createElementNS("http://www.w3.org/2000/svg", "line");
    l.setAttribute("x1", String(t)), l.setAttribute("y1", String(n)), l.setAttribute("x2", String(e)), l.setAttribute("y2", String(i)), l.setAttribute("stroke", s), l.setAttribute("stroke-width", String(a)), this.svg.appendChild(l);
  }
}
const W = class W {
  constructor(t, n) {
    g(this, "level", 100);
    g(this, "target");
    g(this, "onZoomChange");
    this.target = t, this.onZoomChange = n, this.apply(), t.addEventListener("wheel", (e) => {
      (e.ctrlKey || e.metaKey) && (e.preventDefault(), this.setLevel(this.level + (e.deltaY < 0 ? W.STEP : -10)));
    }, { passive: !1 });
  }
  getLevel() {
    return this.level;
  }
  setLevel(t) {
    var n;
    this.level = Math.max(W.MIN, Math.min(W.MAX, t)), this.apply(), (n = this.onZoomChange) == null || n.call(this, this.level);
  }
  zoomIn() {
    this.setLevel(this.level + W.STEP);
  }
  zoomOut() {
    this.setLevel(this.level - W.STEP);
  }
  resetZoom() {
    this.setLevel(100);
  }
  apply() {
    this.target.style.transform = `scale(${this.level / 100})`, this.target.style.transformOrigin = "top center";
  }
};
g(W, "MIN", 50), g(W, "MAX", 200), g(W, "STEP", 10);
let dt = W;
function In(o, t = 14) {
  const n = "http://www.w3.org/2000/svg", e = document.createElementNS(n, "svg");
  e.setAttribute("width", String(t)), e.setAttribute("height", String(t)), e.setAttribute("viewBox", "0 0 24 24"), e.setAttribute("fill", "none"), e.setAttribute("stroke", "currentColor"), e.setAttribute("stroke-width", "1.75"), e.setAttribute("stroke-linecap", "round"), e.setAttribute("stroke-linejoin", "round"), e.style.pointerEvents = "none", e.style.flexShrink = "0";
  for (const [i, s] of o) {
    const a = document.createElementNS(n, i);
    for (const [r, l] of Object.entries(s))
      a.setAttribute(r, l);
    e.appendChild(a);
  }
  return e;
}
function Bn(o) {
  switch (o) {
    case "gdocs":
      return hn;
    case "compact":
      return gn;
    default:
      return he;
  }
}
async function Hn(o) {
  const {
    container: t,
    storagePrefix: n = "documentEditor",
    toolbarPreset: e,
    onReady: i,
    onError: s
  } = o;
  try {
    let a = function(S, M) {
      let R;
      return (...F) => {
        clearTimeout(R), R = setTimeout(() => S(...F), M);
      };
    };
    if (t.innerHTML = '<div class="editor-loading">Loading editor engine...</div>', !window.setDotNetReference) {
      let S = null;
      window.setDotNetReference = (M) => {
        S = M, window.dispatchEvent(new CustomEvent("engine-ready", { detail: M }));
      }, window.getDotNetReference = () => S;
    }
    if (!window.__blazorScriptInjected) {
      window.__blazorScriptInjected = !0;
      const S = "/_framework/";
      await new Promise((M, R) => {
        const F = document.createElement("script");
        F.src = `${S}blazor.webassembly.js`, F.setAttribute("autostart", "true"), F.addEventListener("load", () => M()), F.addEventListener("error", () => R(new Error(`Failed to load Blazor from ${F.src}`))), document.head.appendChild(F);
      });
    }
    const r = new ke();
    await r.waitForReady(), t.innerHTML = "", t.className = "flex flex-col h-screen overflow-hidden bg-white";
    const l = document.createElement("div");
    t.appendChild(l);
    const c = document.createElement("div");
    c.className = "flex flex-1 overflow-hidden", t.appendChild(c);
    const d = document.createElement("div");
    d.className = "flex flex-col flex-1 overflow-hidden", c.appendChild(d);
    const h = document.createElement("div");
    h.className = "flex bg-gray-50 flex-shrink-0", d.appendChild(h);
    const u = document.createElement("div");
    u.className = "w-6 border-b border-r border-gray-200 flex-shrink-0", h.appendChild(u);
    const p = document.createElement("button");
    p.type = "button", p.title = "Toggle margin debug panel (doc vs applied)", p.className = "w-full h-full flex items-center justify-center text-gray-400 hover:bg-gray-100 hover:text-gray-600 transition-colors", p.appendChild(In(jt, 12)), u.appendChild(p);
    const f = document.createElement("div");
    f.className = "flex-1 overflow-hidden", h.appendChild(f);
    const b = document.createElement("div");
    b.className = "flex flex-1 overflow-hidden", d.appendChild(b);
    const w = document.createElement("div");
    b.appendChild(w);
    const k = document.createElement("div");
    b.appendChild(k);
    const m = document.createElement("div");
    m.className = "flex flex-1 overflow-auto bg-[#e8eaed] justify-center", b.appendChild(m);
    const x = document.createElement("div");
    c.appendChild(x);
    const C = document.createElement("div");
    t.appendChild(C);
    const v = new je(m), L = v.getCanvas(), E = new Ge(k, L);
    p.addEventListener("click", () => E.toggle()), localStorage.getItem(`${n}.gridLines`) === "1" && L.classList.add("show-grid"), localStorage.getItem(`${n}.pilcrow`) === "1" && L.classList.add("show-pilcrow");
    let B = !1, N = 1;
    const A = (S, M = !1) => {
      if (S === N && !M) return;
      N = S;
      const R = v.getPageRulerDimensions(S);
      if (!R) return;
      const F = v.getPageTopScrollY(S);
      q.updateDimensions(R.pageWidth, R.marginLeft, R.marginRight), G.updateDimensions(R.pageHeight, R.marginTop, R.marginBottom, S, F);
    };
    let j = (S) => {
    }, J = (S) => {
    }, X = (S) => {
    }, Et = () => {
    };
    const z = (S) => {
      var F;
      B = !0, Ye(), ((F = S.sections) == null ? void 0 : F.length) > 0 && (v.updateFromSections(S.sections), E.update(v.getDebugSectionData())), Pe(S.renderTree, L), He(S.selection, L), Q.updateState(S), X(S.renderTree), j(S.renderTree), J(S.renderTree), Et();
      const M = v.getCurrentPage(m), R = v.getPageForCursor();
      A(R, !0), Y.updatePageInfo(M, v.pageCount), requestAnimationFrame(() => {
        B = !1;
      });
    }, pe = Bn(e), Q = new Sn(l, r, L, z, pe), tt = new kn(x), Y = new Tn(C), q = new Ln(f, r, L, z), G = new Pn(w);
    j = a((S) => tt.updateOutline(S), 300), J = a((S) => tt.updateStats(S), 300), X = a((S) => Y.updateWordCount(S), 300), Et = a(() => {
      v.updatePagination(), G.setPageCount(v.pageCount), G.setTotalScrollHeight(v.getTotalScrollHeight()), A(N, !0);
    }, 150);
    const ue = m.querySelector(".pages-wrapper"), et = new dt(ue, (S) => {
      Y.updateZoom(S), G.setZoom(S / 100), q.setZoom(S / 100);
    }), kt = () => et.zoomOut(), Tt = () => et.zoomIn();
    window.addEventListener("doc:zoom-out", kt), window.addEventListener("doc:zoom-in", Tt), m.addEventListener("scroll", () => {
      G.syncScroll(m.scrollTop), q.syncScrollLeft(m.scrollLeft);
      const S = v.getCurrentPage(m);
      Y.updatePageInfo(S, v.pageCount);
    }), Ue(L, m, r, z);
    const Lt = new nn(L, r, z), Pt = new ln(r, L, z), It = new cn(r, L, z), Bt = new dn(r, L, z), Rt = () => {
      if (B) return;
      const S = window.getSelection();
      !S || !S.isCollapsed || S.rangeCount === 0 || L.contains(S.getRangeAt(0).startContainer) && (v.adjustCursorForPageBreaks(), A(v.getPageForCursor()), E.setCursorSection(v.getSectionForCursor()));
    };
    document.addEventListener("selectionchange", Rt);
    const At = a(async () => {
      if (B || !r.isReady) return;
      const S = window.getSelection();
      if (!S || S.rangeCount === 0 || !L.contains(S.anchorNode)) return;
      const M = T(L);
      if (M)
        try {
          const R = await r.getFormatState(M);
          Q.updateFormatState(R);
        } catch {
        }
    }, 80);
    document.addEventListener("selectionchange", At);
    const me = await r.initialize();
    z(me), L.focus();
    const nt = {
      engine: r,
      destroy() {
        document.removeEventListener("selectionchange", Rt), document.removeEventListener("selectionchange", At), window.removeEventListener("doc:zoom-out", kt), window.removeEventListener("doc:zoom-in", Tt), Pt.destroy(), It.destroy(), Bt.destroy(), Lt.destroy(), t.innerHTML = "", delete window.__documentEditor;
      }
    };
    return window.__documentEditor = {
      engine: r,
      canvas: L,
      toolbar: Q,
      sidebar: tt,
      statusBar: Y,
      rulerH: q,
      rulerV: G,
      zoom: et,
      inputHandler: Pt,
      shortcuts: It,
      pasteHandler: Bt,
      pageLayout: v,
      contextMenu: Lt,
      debugPanel: E,
      instance: nt
    }, i == null || i(nt), nt;
  } catch (a) {
    const r = a instanceof Error ? a : new Error(String(a));
    throw s == null || s(r), r;
  }
}
export {
  Hn as mountEditor
};
//# sourceMappingURL=index.js.map
