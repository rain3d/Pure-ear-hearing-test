/**
 * Audiogram Visualizer (Standard Clinical Audiogram on Canvas)
 * Right Ear: Red 'O' with solid red connecting line
 * Left Ear: Blue 'X' with dashed blue connecting line
 * Normal Hearing Range (0 ~ 25 dB HL) highlighted
 */
import { t } from './i18n.js?v=11';

export class AudiogramRenderer {
  constructor(canvas) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d');
    this.freqs = [250, 500, 1000, 2000, 4000, 8000];
    this.minDb = -10;
    this.maxDb = 60; // Focused on our 0-50 dB HL safety bounds
  }

  resize() {
    const rect = this.canvas.parentElement.getBoundingClientRect();
    const dpr = window.devicePixelRatio || 1;
    this.canvas.width = rect.width * dpr;
    this.canvas.height = 320 * dpr;
    this.canvas.style.width = `${rect.width}px`;
    this.canvas.style.height = `320px`;
    this.ctx.scale(dpr, dpr);
    this.width = rect.width;
    this.height = 320;
  }

  draw(thresholds = []) {
    this.resize();
    const ctx = this.ctx;
    const w = this.width;
    const h = this.height;

    const padL = 45;
    const padR = 25;
    const padT = 30;
    const padB = 40;
    const plotW = w - padL - padR;
    const plotH = h - padT - padB;

    ctx.clearRect(0, 0, w, h);

    // Background
    ctx.fillStyle = '#090D16';
    ctx.fillRect(0, 0, w, h);

    // Coordinate Mappings
    const getX = (freq) => {
      const idx = this.freqs.indexOf(freq);
      if (idx === -1) return padL;
      return padL + (idx / (this.freqs.length - 1)) * plotW;
    };

    const getY = (db) => {
      return padT + ((db - this.minDb) / (this.maxDb - this.minDb)) * plotH;
    };

    // Highlight Normal Hearing Band (-10 ~ 25 dB HL)
    const normalY1 = getY(0);
    const normalY2 = getY(25);
    ctx.fillStyle = 'rgba(16, 185, 129, 0.08)';
    ctx.fillRect(padL, normalY1, plotW, normalY2 - normalY1);

    // Normal hearing label
    ctx.fillStyle = '#10B981';
    ctx.font = '10px sans-serif';
    ctx.fillText(t('canvasNormalBand', '正常聽力範圍 (0 ~ 25 dB HL)'), padL + 8, normalY1 + 14);

    // Draw Grid Lines (Horizontal dB HL: -10, 0, 10, 20, 30, 40, 50, 60)
    ctx.lineWidth = 1;
    ctx.textAlign = 'right';
    ctx.textBaseline = 'middle';
    ctx.font = '11px sans-serif';

    for (let db = 0; db <= 50; db += 10) {
      const y = getY(db);
      ctx.strokeStyle = db === 0 ? '#475569' : '#1E293B';
      ctx.beginPath();
      ctx.moveTo(padL, y);
      ctx.lineTo(padL + plotW, y);
      ctx.stroke();

      ctx.fillStyle = '#64748B';
      ctx.fillText(`${db}`, padL - 8, y);
    }

    // Draw Vertical Frequency Lines
    ctx.textAlign = 'center';
    ctx.textBaseline = 'top';

    this.freqs.forEach((freq) => {
      const x = getX(freq);
      ctx.strokeStyle = freq === 1000 ? '#334155' : '#1E293B';
      ctx.beginPath();
      ctx.moveTo(x, padT);
      ctx.lineTo(x, padT + plotH);
      ctx.stroke();

      ctx.fillStyle = freq === 1000 ? '#38BDF8' : '#94A3B8';
      const label = freq >= 1000 ? `${freq / 1000}k` : `${freq}`;
      ctx.fillText(label, x, padT + plotH + 8);
    });

    // Axis Units
    ctx.fillStyle = '#64748B';
    ctx.font = '10px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(t('canvasFreqAxis', '頻率 (Hz)'), padL + plotW / 2, padT + plotH + 24);

    ctx.save();
    ctx.translate(14, padT + plotH / 2);
    ctx.rotate(-Math.PI / 2);
    ctx.textAlign = 'center';
    ctx.fillText(t('canvasDbAxis', '聽力級 (dB HL)'), 0, 0);
    ctx.restore();

    // Group Thresholds by Ear
    const rightPoints = thresholds
      .filter((t) => t.ear === 'Right')
      .sort((a, b) => a.freq - b.freq);

    const leftPoints = thresholds
      .filter((t) => t.ear === 'Left')
      .sort((a, b) => a.freq - b.freq);

    // Draw Right Ear Line & Red Circles 'O'
    if (rightPoints.length > 0) {
      ctx.strokeStyle = '#EF4444';
      ctx.lineWidth = 2.5;
      ctx.setLineDash([]);
      ctx.beginPath();
      rightPoints.forEach((pt, i) => {
        const x = getX(pt.freq);
        const y = getY(pt.threshold);
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      });
      ctx.stroke();

      rightPoints.forEach((pt) => {
        const x = getX(pt.freq);
        const y = getY(pt.threshold);
        ctx.fillStyle = '#090D16';
        ctx.strokeStyle = '#EF4444';
        ctx.lineWidth = 2.5;
        ctx.beginPath();
        ctx.arc(x, y, 6, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();
      });
    }

    // Draw Left Ear Line & Blue Crosses 'X'
    if (leftPoints.length > 0) {
      ctx.strokeStyle = '#38BDF8';
      ctx.lineWidth = 2.5;
      ctx.setLineDash([5, 4]); // Dashed line for Left Ear
      ctx.beginPath();
      leftPoints.forEach((pt, i) => {
        const x = getX(pt.freq);
        const y = getY(pt.threshold);
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      });
      ctx.stroke();
      ctx.setLineDash([]); // Reset line dash

      leftPoints.forEach((pt) => {
        const x = getX(pt.freq);
        const y = getY(pt.threshold);
        const s = 6;
        ctx.strokeStyle = '#38BDF8';
        ctx.lineWidth = 2.5;
        ctx.beginPath();
        ctx.moveTo(x - s, y - s);
        ctx.lineTo(x + s, y + s);
        ctx.moveTo(x + s, y - s);
        ctx.lineTo(x - s, y + s);
        ctx.stroke();
      });
    }
  }
}
