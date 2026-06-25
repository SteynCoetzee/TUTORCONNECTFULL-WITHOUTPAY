import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../services/report.service';
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);

interface ReportType {
  id: string; name: string; icon: string; description: string;
  chartType: 'bar' | 'line' | 'doughnut' | 'horizontalBar';
  labelKey: string; valueKey: string; valueLabel: string;
  supportsDateRange: boolean;
}

interface RecentReport { name: string; icon: string; generatedOn: string; data: any[]; }

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.css'
})
export class ReportsComponent implements OnInit, OnDestroy {
  selectedReportType = 'tutor-hours';
  errorMessage = '';

  // Date range (shared for standard reports)
  dateFrom = '';
  dateTo   = '';

  // Preview state
  previewData: any[] | null = null;
  previewLoading = false;
  previewTitle = '';
  previewIcon  = '';

  // Chart
  private chart: Chart | null = null;

  // Custom query
  customEntity  = 'bookings';
  customGroupBy = 'month';
  customFrom    = '';
  customTo      = '';
  customData: any[] | null = null;
  customLoading = false;
  customTitle   = '';
  private customChart: Chart | null = null;

  readonly reportTypes: ReportType[] = [
    { id: 'tutor-hours',      name: 'Tutor Hours',        icon: 'timer',          description: 'Total logged hours per tutor',                   chartType: 'bar',           labelKey: 'tutorName',    valueKey: 'totalHoursWorked', valueLabel: 'Hours',       supportsDateRange: true  },
    { id: 'tutor-ratings',    name: 'Tutor Ratings',      icon: 'star',           description: 'Average ratings per tutor',                      chartType: 'horizontalBar', labelKey: 'tutorName',    valueKey: 'averageRating',    valueLabel: 'Avg Rating',  supportsDateRange: false },
    { id: 'monthly-income',   name: 'Monthly Income',     icon: 'payments',       description: 'Total paid income per month',                    chartType: 'line',          labelKey: 'period',       valueKey: 'totalIncome',      valueLabel: 'Income (R)',  supportsDateRange: true  },
    { id: 'monthly-students', name: 'Monthly Students',   icon: 'people',         description: 'Unique students with bookings per month',        chartType: 'line',          labelKey: 'period',       valueKey: 'uniqueStudents',   valueLabel: 'Students',    supportsDateRange: true  },
    { id: 'sessions',         name: 'Sessions',           icon: 'calendar_today', description: 'All booked sessions with date, time & type',     chartType: 'doughnut',      labelKey: 'sessionType',  valueKey: 'sessionType',      valueLabel: 'Sessions',    supportsDateRange: true  },
    { id: 'popular-modules',  name: 'Popular Modules',    icon: 'menu_book',      description: 'Module popularity by announcement activity',     chartType: 'horizontalBar', labelKey: 'moduleName',   valueKey: 'announcementCount',valueLabel: 'Announcements',supportsDateRange: true  },
  ];

  readonly customEntities = [
    { id: 'bookings',    label: 'Bookings',    groupBys: [{ id: 'month', label: 'By Month' }, { id: 'type', label: 'By Session Type' }] },
    { id: 'enrollments', label: 'Enrollments', groupBys: [{ id: 'month', label: 'By Month' }, { id: 'module', label: 'By Module' }] },
    { id: 'payments',    label: 'Payments',    groupBys: [{ id: 'month', label: 'By Month' }, { id: 'status', label: 'By Status' }] },
    { id: 'users',       label: 'Users',       groupBys: [{ id: 'role',  label: 'By Role' }] },
  ];

  recentReports: RecentReport[] = [];

  constructor(private reportService: ReportService) {}

  ngOnInit() {
    const def = this.reportTypes.find(r => r.id === this.selectedReportType)!;
    this.selectReport(def);
  }

  ngOnDestroy() {
    this.chart?.destroy();
    this.customChart?.destroy();
  }

  get selectedReport(): ReportType | undefined {
    return this.reportTypes.find(r => r.id === this.selectedReportType);
  }

  get previewColumns(): string[] {
    return this.previewData?.length ? Object.keys(this.previewData[0]) : [];
  }

  get customColumns(): string[] {
    return this.customData?.length ? Object.keys(this.customData[0]) : [];
  }

  get currentEntityGroupBys() {
    return this.customEntities.find(e => e.id === this.customEntity)?.groupBys ?? [];
  }

  onEntityChange() {
    this.customGroupBy = this.currentEntityGroupBys[0]?.id ?? 'month';
  }

  // ── Standard report selection ───────────────────────────────────────────────
  selectReport(rt: ReportType) {
    this.selectedReportType = rt.id;
    this.previewData = null;
    this.previewTitle = rt.name;
    this.previewIcon  = rt.icon;
    this.errorMessage = '';
    this.previewLoading = true;
    this.chart?.destroy();
    this.chart = null;

    this.getObs(rt.id).subscribe({
      next: (data) => {
        this.previewLoading = false;
        this.previewData = data ?? [];
        if (!data?.length) { this.errorMessage = 'No data for this report yet.'; return; }
        requestAnimationFrame(() => this.renderChart(data, rt));
      },
      error: () => { this.previewLoading = false; this.errorMessage = 'Failed to load report.'; }
    });
  }

  applyDateRange() {
    const rt = this.selectedReport;
    if (rt) this.selectReport(rt);
  }

  // ── Custom query ────────────────────────────────────────────────────────────
  runCustomQuery() {
    this.customData = null;
    this.customLoading = true;
    this.customTitle = `${this.customEntities.find(e => e.id === this.customEntity)?.label} — ${this.currentEntityGroupBys.find(g => g.id === this.customGroupBy)?.label}`;
    this.customChart?.destroy();
    this.customChart = null;

    this.reportService.getCustomReport(this.customEntity, this.customGroupBy, this.customFrom || undefined, this.customTo || undefined).subscribe({
      next: (data) => {
        this.customLoading = false;
        this.customData = data ?? [];
        if (data?.length) requestAnimationFrame(() => this.renderCustomChart(data));
      },
      error: () => { this.customLoading = false; this.errorMessage = 'Custom query failed.'; }
    });
  }

  // ── Download ────────────────────────────────────────────────────────────────
  downloadPreview() {
    if (!this.previewData?.length) return;
    const base = this.selectedReportType + '-report';
    this.downloadCsv(this.toCsv(this.previewData), `${base}.csv`);
    this.downloadChartPng(this.chart, `${base}-chart.png`);
    const date = new Date().toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' });
    const entry = { name: `${this.previewTitle} — ${date}`, icon: this.previewIcon, generatedOn: date, data: this.previewData };
    if (!this.recentReports.some(r => r.name === entry.name)) this.recentReports.unshift(entry);
  }

  downloadCustom() {
    if (!this.customData?.length) return;
    const base = `custom-${this.customEntity}-report`;
    this.downloadCsv(this.toCsv(this.customData), `${base}.csv`);
    this.downloadChartPng(this.customChart, `${base}-chart.png`);
  }

  downloadRecent(r: RecentReport) {
    this.downloadCsv(this.toCsv(r.data), `${r.name.replace(/[\s—]/g, '-')}.csv`);
  }

  private downloadChartPng(chart: Chart | null, filename: string) {
    if (!chart) return;
    const a = document.createElement('a');
    a.href = chart.toBase64Image('image/png', 1);
    a.download = filename;
    a.click();
  }

  // ── Chart rendering ─────────────────────────────────────────────────────────
  private COLORS = ['#5bbfba','#2d3748','#10b981','#f59e0b','#3b82f6','#8b5cf6','#ef4444','#ec4899'];

  private renderChart(data: any[], rt: ReportType) {
    const canvas = document.getElementById('reportChart') as HTMLCanvasElement;
    if (!canvas) return;
    this.chart?.destroy();
    const w = canvas.parentElement?.clientWidth || 600;
    canvas.width  = w;
    canvas.height = Math.round(w / (rt.chartType === 'doughnut' ? 2 : rt.chartType === 'horizontalBar' ? 1.8 : 2.8));

    let labels: string[];
    let values: number[];
    let type: 'bar' | 'line' | 'doughnut';

    if (rt.id === 'sessions') {
      // Aggregate by session type for doughnut
      const counts: Record<string, number> = {};
      data.forEach(d => { const k = d.sessionType || 'Unknown'; counts[k] = (counts[k] || 0) + 1; });
      labels = Object.keys(counts);
      values = Object.values(counts);
      type = 'doughnut';
    } else {
      labels = data.map(d => String(d[rt.labelKey] ?? ''));
      values = data.map(d => Number(d[rt.valueKey] ?? 0));
      type = rt.chartType === 'horizontalBar' ? 'bar' : rt.chartType as 'bar' | 'line';
    }

    const isHoriz = rt.chartType === 'horizontalBar';
    const isLine  = type === 'line';
    const isDough = type === 'doughnut';

    this.chart = new Chart(canvas, {
      type,
      data: {
        labels,
        datasets: [{
          label: rt.valueLabel,
          data: values,
          backgroundColor: isDough ? this.COLORS.slice(0, labels.length) : isLine ? 'rgba(91,191,186,0.15)' : '#5bbfba',
          borderColor: isDough ? this.COLORS.slice(0, labels.length) : '#5bbfba',
          borderWidth: isDough ? 2 : isLine ? 2 : 0,
          fill: isLine,
          tension: 0.4,
          borderRadius: isLine || isDough ? 0 : 6,
          pointBackgroundColor: '#5bbfba',
          pointRadius: isLine ? 4 : 0,
        }]
      },
      options: {
        responsive: false,
        maintainAspectRatio: false,
        indexAxis: isHoriz ? 'y' : 'x',
        plugins: {
          legend: { display: isDough, position: 'right' },
          tooltip: { callbacks: {
            label: (ctx) => {
              const v = ctx.parsed[isHoriz ? 'x' : 'y'];
              if (rt.id === 'monthly-income') return ` R ${Number(v).toLocaleString('en-ZA', { minimumFractionDigits: 2 })}`;
              if (rt.id === 'tutor-ratings') return ` ${v} / 5`;
              return ` ${v}`;
            }
          }}
        },
        scales: isDough ? {} : {
          x: { grid: { color: '#f0f0f0' }, ticks: { color: '#6b7280', font: { size: 11 } } },
          y: { grid: { color: '#f0f0f0' }, ticks: { color: '#6b7280', font: { size: 11 } }, beginAtZero: true }
        }
      }
    });
  }

  private renderCustomChart(data: any[]) {
    const canvas = document.getElementById('customChart') as HTMLCanvasElement;
    if (!canvas) return;
    this.customChart?.destroy();
    const w = canvas.parentElement?.clientWidth || 400;
    canvas.width  = w;
    canvas.height = Math.round(w / 2);
    const labels = data.map(d => String(d['category'] ?? d['Category'] ?? ''));
    const values = data.map(d => Number(d['count'] ?? d['Count'] ?? d['total'] ?? d['Total'] ?? 0));
    this.customChart = new Chart(canvas, {
      type: 'bar',
      data: {
        labels,
        datasets: [{ label: 'Count', data: values, backgroundColor: '#2d3748', borderRadius: 6 }]
      },
      options: {
        responsive: false,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          x: { grid: { color: '#f0f0f0' }, ticks: { color: '#6b7280', font: { size: 11 } } },
          y: { grid: { color: '#f0f0f0' }, ticks: { color: '#6b7280', font: { size: 11 } }, beginAtZero: true }
        }
      }
    });
  }

  // ── Helpers ─────────────────────────────────────────────────────────────────
  private getObs(id: string) {
    const f = this.dateFrom || undefined;
    const t = this.dateTo   || undefined;
    switch (id) {
      case 'tutor-hours':      return this.reportService.getTutorHoursReport(f, t);
      case 'tutor-ratings':    return this.reportService.getTutorRatingsReport();
      case 'monthly-income':   return this.reportService.getMonthlyIncome(f, t);
      case 'monthly-students': return this.reportService.getMonthlyStudentsReport(f, t);
      case 'sessions':         return this.reportService.getSessionsReport(f, t);
      case 'popular-modules':  return this.reportService.getPopularModulesReport(f, t);
      default:                 return this.reportService.getTutorHoursReport(f, t);
    }
  }

  toCsv(data: any[]): string {
    if (!data?.length) return '';
    const headers = Object.keys(data[0]).join(',');
    const rows = data.map(row => Object.values(row).map(v => `"${String(v ?? '').replace(/"/g, '""')}"`).join(','));
    return [headers, ...rows].join('\n');
  }

  downloadCsv(csv: string, filename: string) {
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = filename; a.click();
    URL.revokeObjectURL(url);
  }

  formatVal(v: any): string {
    if (v == null) return '—';
    if (typeof v === 'number' && String(v).includes('.')) return Number(v).toFixed(2);
    return String(v);
  }
}
