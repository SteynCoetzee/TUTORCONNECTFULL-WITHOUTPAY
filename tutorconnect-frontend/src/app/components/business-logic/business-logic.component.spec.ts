import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { BusinessLogicComponent } from './business-logic.component';

describe('BusinessLogicComponent — Admin System Configuration', () => {
  let component: BusinessLogicComponent;
  let httpMock: HttpTestingController;

  const mockRule = {
    rule_ID: 1,
    rule_Name: 'session_timeout_minutes',
    rule_Value: 30,
    description: 'Minutes of inactivity before automatic logout'
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BusinessLogicComponent, HttpClientTestingModule]
    }).compileComponents();

    const fixture = TestBed.createComponent(BusinessLogicComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);

    // Trigger ngOnInit → loadRules() → GET /BusinessRules
    fixture.detectChanges();
    httpMock.expectOne(req => req.url.includes('BusinessRules')).flush([mockRule]);
  });

  afterEach(() => httpMock.verify());

  // ── getMeta ───────────────────────────────────────────────────────────────────

  it('should return correct metadata for the session_timeout_minutes rule', () => {
    const meta = component.getMeta('session_timeout_minutes');
    expect(meta.label).toBe('AFK Session Timeout');
    expect(meta.unit).toBe('minutes');
    expect(meta.min).toBe(1);
    expect(meta.max).toBe(480);
    expect(meta.icon).toBe('timer_off');
  });

  it('should return generic fallback metadata for an unrecognised rule name', () => {
    const meta = component.getMeta('unknown_future_rule');
    expect(meta.label).toBe('unknown_future_rule');
    expect(meta.icon).toBe('settings');
    expect(meta.min).toBe(0);
    expect(meta.max).toBe(9999);
  });

  // ── isDirty ───────────────────────────────────────────────────────────────────

  it('should mark a rule as dirty when the edit value differs from the saved value', () => {
    component.editValues[1] = 45; // loaded value was 30
    expect(component.isDirty(mockRule)).toBeTrue();
  });

  it('should mark a rule as clean when the edit value matches the saved value', () => {
    // editValues[1] = 30 was set by loadRules in beforeEach
    expect(component.isDirty(mockRule)).toBeFalse();
  });

  // ── saveRule validation ───────────────────────────────────────────────────────

  it('should show a range error and make no HTTP call when the value is below the minimum', () => {
    component.editValues[1] = 0; // minimum is 1
    component.saveRule(mockRule);
    expect(component.errorMessage).toContain('between 1 and 480');
    // httpMock.verify() in afterEach will catch any unexpected PUT requests
  });

  it('should show a range error and make no HTTP call when the value exceeds the maximum', () => {
    component.editValues[1] = 999; // maximum is 480
    component.saveRule(mockRule);
    expect(component.errorMessage).toContain('between 1 and 480');
  });

  it('should clear any previous error message when a valid save request is made', () => {
    component.errorMessage = 'Old error';
    component.editValues[1] = 60; // valid value
    component.saveRule(mockRule);
    expect(component.errorMessage).toBe('');
    // Flush the PUT to prevent verify() failure
    httpMock.expectOne(req => req.method === 'PUT').flush({});
  });

  it('should set saving state to true while the PUT request is in flight', () => {
    component.editValues[1] = 60;
    component.saveRule(mockRule);
    expect(component.saving[1]).toBeTrue();
    httpMock.expectOne(req => req.method === 'PUT').flush({});
    expect(component.saving[1]).toBeFalse();
  });
});
