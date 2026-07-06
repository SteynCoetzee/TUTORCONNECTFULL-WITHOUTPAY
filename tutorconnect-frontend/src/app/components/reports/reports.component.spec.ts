import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ReportsComponent } from './reports.component';

describe('ReportsComponent — Report Data Generation & CSV Export', () => {
  let component: ReportsComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReportsComponent, HttpClientTestingModule]
    }).compileComponents();

    const fixture = TestBed.createComponent(ReportsComponent);
    component = fixture.componentInstance;
    // Do not call detectChanges — avoids Chart.js canvas instantiation in JSDOM
  });

  // ── toCsv ────────────────────────────────────────────────────────────────────

  it('should return an empty string when given an empty array', () => {
    expect(component.toCsv([])).toBe('');
  });

  it('should generate a header row from the object keys of the first record', () => {
    const csv = component.toCsv([{ tutorName: 'Alice', totalHoursWorked: 10 }]);
    expect(csv.split('\n')[0]).toBe('tutorName,totalHoursWorked');
  });

  it('should wrap all data values in double quotes', () => {
    const csv = component.toCsv([{ name: 'Alice', hours: 10 }]);
    expect(csv.split('\n')[1]).toBe('"Alice","10"');
  });

  it('should produce exactly two lines (header + data) for a single-record array', () => {
    const csv = component.toCsv([{ module: 'INF370', count: 5 }]);
    expect(csv.split('\n').length).toBe(2);
  });

  it('should produce a header and one row per record for multi-record arrays', () => {
    const data = [
      { tutor: 'Alice', hours: 10 },
      { tutor: 'Bob',   hours: 8  },
      { tutor: 'Carol', hours: 12 }
    ];
    const lines = component.toCsv(data).split('\n');
    expect(lines.length).toBe(4); // 1 header + 3 rows
  });

  it('should escape double-quote characters inside values by doubling them', () => {
    const csv = component.toCsv([{ name: 'O"Brien', score: 5 }]);
    expect(csv).toContain('"O""Brien"');
  });

  it('should render null values as empty quoted strings', () => {
    const csv = component.toCsv([{ name: null, score: 0 }]);
    expect(csv.split('\n')[1]).toContain('""');
  });

  // ── formatVal ────────────────────────────────────────────────────────────────

  it('should display null as an em-dash', () => {
    expect(component.formatVal(null)).toBe('—');
  });

  it('should display undefined as an em-dash', () => {
    expect(component.formatVal(undefined)).toBe('—');
  });

  it('should display an integer without any decimal formatting', () => {
    expect(component.formatVal(42)).toBe('42');
  });

  it('should format a decimal number to exactly two decimal places', () => {
    expect(component.formatVal(4.5678)).toBe('4.57');
  });

  it('should pass string values through unchanged', () => {
    expect(component.formatVal('One-on-One')).toBe('One-on-One');
  });
});
