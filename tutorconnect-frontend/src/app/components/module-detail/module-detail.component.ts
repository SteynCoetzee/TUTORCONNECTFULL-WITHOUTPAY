import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { Module, ModuleResource, ModuleAssignment, ModuleQuiz, TutorModuleAssignment, UserProfile, Announcement } from '../../models/models';
import { environment } from '../../../environments/environment';

interface BuilderQuestion {
  question_Text: string;
  question_Order: number;
  options: BuilderOption[];
}
interface BuilderOption {
  option_Text: string;
  is_Correct: boolean;
}

@Component({
  selector: 'app-module-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, NgClass],
  templateUrl: './module-detail.component.html',
  styleUrl: './module-detail.component.css'
})
export class ModuleDetailComponent implements OnInit {
  moduleCode = '';
  module: Module | null = null;
  role = '';
  userId = 0;
  isAssigned = false;
  loading = true;
  errorMessage = '';
  successMessage = '';

  activeTab: 'resources' | 'quizzes' | 'assignments' | 'announcements' | 'tutors' = 'resources';

  resources: ModuleResource[] = [];
  quizzes: ModuleQuiz[] = [];
  assignments: ModuleAssignment[] = [];
  assignedTutors: TutorModuleAssignment[] = [];
  allTutors: UserProfile[] = [];

  // Resource form (inline)
  showResourceForm = false;
  resFolder = '';
  resTitle = '';
  resType: 'PDF' | 'Link' | 'Video' | '' = '';
  resUrl = '';
  resFile: File | null = null;
  resUploading = false;
  savingResource = false;
  resourceSearch = '';
  togglingVisId: number | null = null;
  collapsedFolders = new Set<string>();

  // Resource editing
  editingResourceId: number | null = null;
  editResTitle = '';
  editResFolder = '';
  editResType: 'PDF' | 'Link' | 'Video' | '' = '';
  editResUrl = '';
  editResFile: File | null = null;
  editResUploading = false;
  savingEditResource = false;

  // Quiz form (inline)
  showQuizForm = false;
  quizName = '';
  quizDetails = '';
  quizDate = '';
  savingQuiz = false;
  togglingQuizVisId: number | null = null;

  // Quiz submissions (tutor)
  submissionsQuizId: number | null = null;
  submissions: any[] = [];
  loadingSubmissions = false;

  // Quiz builder (tutor)
  builderQuizId: number | null = null;
  builderQuestions: BuilderQuestion[] = [];
  savingQuestions = false;

  // Quiz taking (student)
  takingQuizId: number | null = null;
  takingQuiz: any = null;
  takingAnswers: { [questionId: number]: number } = {};
  startingQuiz = false;
  submittingQuiz = false;
  quizResult: { score: number; correct: number; total: number } | null = null;

  // Assignment form (inline)
  showAssignmentForm = false;
  assignmentName = '';
  assignmentDate = '';
  assignmentBriefFile: File | null = null;
  uploadingBrief = false;
  savingAssignment = false;
  togglingAssignmentVisId: number | null = null;

  // Assignment submissions (tutor)
  assignmentSubmissionsId: number | null = null;
  assignmentSubmissions: any[] = [];
  loadingAssignmentSubmissions = false;
  gradingSubmissionId: number | null = null;
  gradeValue: number | null = null;
  feedbackValue = '';
  savingGrade = false;

  // Student submitting (inline form)
  studentSubmitAssignmentId: number | null = null;
  studentSubmitTitle = '';
  studentSubmitFile: File | null = null;
  submittingAssignmentId: number | null = null;


  // Delete confirmations
  deleteResourceId: number | null = null;
  deleteQuizId: number | null = null;
  deleteAssignmentId: number | null = null;

  // Announcements
  announcements: Announcement[] = [];
  showAnnouncementCreateForm = false;
  announcementName = '';
  announcementDetails = '';
  announcementType = 'Update';
  announcementTypes = ['Update', 'Deadline', 'Event', 'Resource'];
  savingAnnouncement = false;
  editingAnnouncement: Announcement | null = null;
  editAnnouncementName = '';
  editAnnouncementDetails = '';
  editAnnouncementType = 'Update';
  updatingAnnouncement = false;
  deleteAnnouncementId: number | null = null;
  deletingAnnouncement = false;

  // Admin: assign tutor
  selectedTutorId: number | null = null;
  assigningTutor = false;

  private apiUrl = environment.apiUrl;

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  ngOnInit() {
    this.role = this.authService.getCurrentUserRole();
    this.userId = this.authService.getCurrentUserId() ?? 0;
    this.moduleCode = this.route.snapshot.paramMap.get('code') ?? '';

    if (!this.moduleCode) {
      this.router.navigate(['/dashboard/courses']);
      return;
    }

    this.loadModule();
    this.loadResources();
    this.loadQuizzes();
    this.loadAssignments();
    this.loadAnnouncements();

    if (this.role === 'Tutor') {
      this.checkTutorAssignment();
    }
    if (this.role === 'Admin') {
      this.isAssigned = true;
      this.loadAssignedTutors();
      this.loadAllTutors();
    }
  }

  loadModule() {
    this.http.get<Module[]>(`${this.apiUrl}/Modules`).subscribe({
      next: (modules) => {
        this.module = modules.find(m => m.module_Code === this.moduleCode) ?? null;
        this.loading = false;
      },
      error: () => { this.errorMessage = 'Failed to load module.'; this.loading = false; }
    });
  }

  loadResources() {
    this.http.get<ModuleResource[]>(`${this.apiUrl}/ModuleResources/module/${this.moduleCode}`).subscribe({
      next: (data) => {
        this.resources = data;
        // Default all folders to collapsed
        const folders = new Set(data.map(r => r.folder_Name?.trim() || 'General'));
        this.collapsedFolders = new Set(folders);
      },
      error: () => {}
    });
  }

  loadQuizzes() {
    const url = this.role === 'Student'
      ? `${this.apiUrl}/Quizzes/module/${this.moduleCode}?studentId=${this.userId}`
      : `${this.apiUrl}/Quizzes/module/${this.moduleCode}`;
    this.http.get<ModuleQuiz[]>(url).subscribe({
      next: (data) => { this.quizzes = data; },
      error: () => {}
    });
  }

  loadAssignments() {
    const url = this.role === 'Student'
      ? `${this.apiUrl}/Assignments/module/${this.moduleCode}?studentId=${this.userId}`
      : `${this.apiUrl}/Assignments/module/${this.moduleCode}`;
    this.http.get<ModuleAssignment[]>(url).subscribe({
      next: (data) => { this.assignments = data; },
      error: () => {}
    });
  }

  checkTutorAssignment() {
    this.http.get<{ isAssigned: boolean }>(`${this.apiUrl}/TutorModule/check/${this.userId}/${this.moduleCode}`).subscribe({
      next: (res) => { this.isAssigned = res.isAssigned; },
      error: () => { this.isAssigned = false; }
    });
  }

  loadAssignedTutors() {
    this.http.get<TutorModuleAssignment[]>(`${this.apiUrl}/TutorModule/module/${this.moduleCode}`).subscribe({
      next: (data) => { this.assignedTutors = data; },
      error: () => {}
    });
  }

  loadAllTutors() {
    this.http.get<UserProfile[]>(`${this.apiUrl}/Users`).subscribe({
      next: (users) => { this.allTutors = users.filter(u => u.roleName === 'Tutor' || u.user_Role_ID === 2); },
      error: () => {}
    });
  }

  goBack() {
    this.router.navigate(['/dashboard/courses']);
  }

  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }

  // ─── Resources ──────────────────────────────────────────────────────────────

  openResourceForm() {
    this.resFolder = '';
    this.resTitle = '';
    this.resType = '';
    this.resUrl = '';
    this.resFile = null;
    this.showResourceForm = true;
    this.clearMessages();
  }

  cancelResourceForm() {
    this.showResourceForm = false;
    this.clearMessages();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.resFile = input.files?.[0] ?? null;
  }

  submitResource() {
    if (!this.resTitle.trim() || !this.resType) {
      this.errorMessage = 'Title and type are required.'; return;
    }
    if (!this.resUrl.trim() && !this.resFile) {
      this.errorMessage = 'Please provide a URL or upload a file.'; return;
    }
    if (this.resFile) {
      this.resUploading = true;
      const fd = new FormData();
      fd.append('file', this.resFile);
      this.http.post(`${this.apiUrl}/ModuleResources/upload`, fd, { responseType: 'text' }).subscribe({
        next: (url) => { this.resUploading = false; this.resUrl = this.parseUrl(url); this.doSaveResource(); },
        error: () => { this.resUploading = false; this.errorMessage = 'File upload failed.'; }
      });
    } else {
      this.doSaveResource();
    }
  }

  private doSaveResource() {
    this.savingResource = true;
    this.clearMessages();
    this.http.post(`${this.apiUrl}/ModuleResources`, {
      module_Resource_Name: this.resTitle.trim(),
      module_Resource_Type_ID: this.resType,
      module_Code: this.moduleCode,
      resource_URL: this.resUrl.trim(),
      folder_Name: this.resFolder.trim()
    }).subscribe({
      next: () => {
        this.savingResource = false;
        this.successMessage = 'Resource added!';
        this.showResourceForm = false;
        this.loadResources();
      },
      error: () => { this.savingResource = false; this.errorMessage = 'Failed to add resource.'; }
    });
  }

  toggleVisibility(res: ModuleResource) {
    this.togglingVisId = res.module_Resource_ID;
    this.http.put(`${this.apiUrl}/ModuleResources/${res.module_Resource_ID}/visibility`,
      { is_Visible: !res.is_Visible }
    ).subscribe({
      next: () => { res.is_Visible = !res.is_Visible; this.togglingVisId = null; },
      error: () => { this.togglingVisId = null; }
    });
  }

  // ─── Folder visibility ───────────────────────────────────────────────────────

  isFolderVisible(folder: string): boolean {
    const items = this.resources.filter(r => (r.folder_Name?.trim() || 'General') === folder);
    return items.length > 0 && items[0].folder_Is_Visible;
  }

  toggleFolderVisibility(folder: string, event: Event) {
    event.stopPropagation();
    const makeVisible = !this.isFolderVisible(folder);
    const folderName = folder === 'General' ? '' : folder;

    this.http.put(`${this.apiUrl}/ModuleResources/folder-visibility`, {
      module_Code: this.moduleCode,
      folder_Name: folderName,
      is_Visible: makeVisible
    }).subscribe({
      next: () => {
        // Update local state:
        // hide → set folder_Is_Visible=false and Is_Visible=false for all
        // show → set folder_Is_Visible=true only, files keep their own Is_Visible
        this.resources
          .filter(r => (r.folder_Name?.trim() || 'General') === folder)
          .forEach(r => {
            r.folder_Is_Visible = makeVisible;
            if (!makeVisible) r.is_Visible = false;
          });
      }
    });
  }

  // ─── Resource editing ────────────────────────────────────────────────────────

  openEditResource(res: ModuleResource, event: Event) {
    event.stopPropagation();
    this.editingResourceId = res.module_Resource_ID;
    this.editResTitle  = res.module_Resource_Name;
    this.editResFolder = res.folder_Name || '';
    this.editResType   = res.module_Resource_Type_ID as 'PDF' | 'Link' | 'Video';
    this.editResUrl    = res.resource_URL || '';
    this.editResFile   = null;
    this.deleteResourceId = null;
    this.clearMessages();
  }

  cancelEditResource() { this.editingResourceId = null; this.clearMessages(); }

  submitEditResource() {
    if (!this.editResTitle.trim() || !this.editResType) {
      this.errorMessage = 'Title and type are required.'; return;
    }
    if (this.editResFile) {
      this.editResUploading = true;
      const fd = new FormData();
      fd.append('file', this.editResFile);
      this.http.post(`${this.apiUrl}/ModuleResources/upload`, fd, { responseType: 'text' }).subscribe({
        next: (url) => { this.editResUploading = false; this.editResUrl = this.parseUrl(url); this.doSaveEdit(); },
        error: () => { this.editResUploading = false; this.errorMessage = 'File upload failed.'; }
      });
    } else {
      this.doSaveEdit();
    }
  }

  private doSaveEdit() {
    this.savingEditResource = true;
    this.http.put(`${this.apiUrl}/ModuleResources/${this.editingResourceId}`, {
      module_Resource_Name: this.editResTitle.trim(),
      module_Resource_Type_ID: this.editResType,
      module_Code: this.moduleCode,
      resource_URL: this.editResUrl.trim(),
      folder_Name: this.editResFolder.trim()
    }).subscribe({
      next: () => {
        this.savingEditResource = false;
        this.editingResourceId = null;
        this.successMessage = 'Resource updated!';
        this.loadResources();
      },
      error: () => { this.savingEditResource = false; this.errorMessage = 'Failed to update resource.'; }
    });
  }

  onEditFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.editResFile = input.files?.[0] ?? null;
  }

  confirmDeleteResource(id: number) { this.deleteResourceId = id; this.clearMessages(); }

  deleteResource() {
    if (!this.deleteResourceId) return;
    this.http.delete(`${this.apiUrl}/ModuleResources/${this.deleteResourceId}`).subscribe({
      next: () => {
        this.successMessage = 'Resource deleted.';
        this.deleteResourceId = null;
        this.loadResources();
      },
      error: () => { this.errorMessage = 'Failed to delete resource.'; this.deleteResourceId = null; }
    });
  }

  // ─── Resource helpers ────────────────────────────────────────────────────────

  get filteredResources(): ModuleResource[] {
    const src = this.role === 'Student'
      ? this.resources.filter(r => r.folder_Is_Visible && r.is_Visible)
      : this.resources;
    const q = this.resourceSearch.toLowerCase().trim();
    if (!q) return src;
    return src.filter(r =>
      r.module_Resource_Name.toLowerCase().includes(q) ||
      (r.folder_Name || '').toLowerCase().includes(q)
    );
  }

  get resourceFolders(): string[] {
    const all = this.filteredResources.map(r => r.folder_Name?.trim() || 'General');
    const unique = [...new Set(all)];
    return unique.sort((a, b) => a === 'General' ? -1 : b === 'General' ? 1 : a.localeCompare(b));
  }

  getResourcesByFolder(folder: string): ModuleResource[] {
    return this.filteredResources.filter(r => (r.folder_Name?.trim() || 'General') === folder);
  }

  isFolderCollapsed(folder: string): boolean {
    return this.collapsedFolders.has(folder);
  }

  toggleFolder(folder: string) {
    if (this.collapsedFolders.has(folder)) {
      this.collapsedFolders.delete(folder);
    } else {
      this.collapsedFolders.add(folder);
    }
    this.collapsedFolders = new Set(this.collapsedFolders);
  }

  resIcon(type: string): string {
    if (type === 'PDF') return 'picture_as_pdf';
    if (type === 'Video') return 'smart_display';
    if (type === 'Link') return 'link';
    return 'description';
  }

  resColor(type: string): string {
    if (type === 'PDF') return '#ef4444';
    if (type === 'Video') return '#8b5cf6';
    if (type === 'Link') return '#3b82f6';
    return '#6b7280';
  }

  // ─── Quizzes ────────────────────────────────────────────────────────────────

  openQuizForm() {
    this.quizName = '';
    this.quizDetails = '';
    this.quizDate = '';
    this.showQuizForm = true;
    this.clearMessages();
  }

  saveQuiz() {
    if (!this.quizName || !this.quizDate) { this.errorMessage = 'Quiz name and date are required.'; return; }
    this.savingQuiz = true;
    this.http.post(`${this.apiUrl}/Quizzes`, {
      quiz_Name: this.quizName,
      quiz_Details: this.quizDetails,
      quiz_Date: this.quizDate,
      module_Code: this.moduleCode
    }).subscribe({
      next: () => {
        this.savingQuiz = false;
        this.successMessage = 'Quiz added!';
        this.showQuizForm = false;
        this.loadQuizzes();
      },
      error: () => { this.savingQuiz = false; this.errorMessage = 'Failed to add quiz.'; }
    });
  }

  confirmDeleteQuiz(id: number) { this.deleteQuizId = id; this.clearMessages(); }

  deleteQuiz() {
    if (!this.deleteQuizId) return;
    this.http.delete(`${this.apiUrl}/Quizzes/${this.deleteQuizId}`).subscribe({
      next: () => {
        this.successMessage = 'Quiz deleted.';
        this.deleteQuizId = null;
        this.loadQuizzes();
      },
      error: () => { this.errorMessage = 'Failed to delete quiz.'; this.deleteQuizId = null; }
    });
  }

  toggleQuizVisibility(quiz: ModuleQuiz) {
    this.togglingQuizVisId = quiz.quiz_ID;
    this.http.put(`${this.apiUrl}/Quizzes/${quiz.quiz_ID}/visibility`,
      { is_Visible: !quiz.is_Visible }
    ).subscribe({
      next: () => { quiz.is_Visible = !quiz.is_Visible; this.togglingQuizVisId = null; },
      error: () => { this.togglingQuizVisId = null; }
    });
  }

  get visibleQuizzes(): ModuleQuiz[] {
    return this.role === 'Student' ? this.quizzes.filter(q => q.is_Visible) : this.quizzes;
  }

  // ─── Quiz builder ────────────────────────────────────────────────────────────

  openBuilder(quiz: ModuleQuiz) {
    this.builderQuizId = quiz.quiz_ID;
    this.quizResult = null;
    this.clearMessages();
    this.http.get<any>(`${this.apiUrl}/Quizzes/${quiz.quiz_ID}/questions`).subscribe({
      next: (data) => {
        this.builderQuestions = (data.questions || []).map((q: any) => ({
          question_Text: q.question_Text,
          question_Order: q.question_Order,
          options: (q.options || []).map((o: any) => ({ option_Text: o.option_Text, is_Correct: o.is_Correct }))
        }));
        if (this.builderQuestions.length === 0) this.addBuilderQuestion();
      },
      error: () => { if (this.builderQuestions.length === 0) this.addBuilderQuestion(); }
    });
  }

  closeBuilder() { this.builderQuizId = null; this.builderQuestions = []; }

  openSubmissions(quiz: ModuleQuiz) {
    this.submissionsQuizId = quiz.quiz_ID;
    this.builderQuizId = null;
    this.loadingSubmissions = true;
    this.submissions = [];
    this.clearMessages();
    this.http.get<any[]>(`${this.apiUrl}/Quizzes/${quiz.quiz_ID}/submissions`).subscribe({
      next: (data) => { this.submissions = data; this.loadingSubmissions = false; },
      error: () => { this.loadingSubmissions = false; this.errorMessage = 'Failed to load submissions.'; }
    });
  }

  closeSubmissions() { this.submissionsQuizId = null; this.submissions = []; }

  formatDuration(seconds: number | null): string {
    if (seconds == null) return '—';
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return m > 0 ? `${m}m ${s}s` : `${s}s`;
  }

  getScoreClass(score: number): string {
    if (score >= 75) return 'score-high';
    if (score >= 50) return 'score-mid';
    return 'score-low';
  }

  addBuilderQuestion() {
    this.builderQuestions.push({
      question_Text: '',
      question_Order: this.builderQuestions.length,
      options: [{ option_Text: '', is_Correct: false }, { option_Text: '', is_Correct: false }]
    });
  }

  removeBuilderQuestion(index: number) { this.builderQuestions.splice(index, 1); }

  addBuilderOption(qi: number) {
    if (this.builderQuestions[qi].options.length < 5)
      this.builderQuestions[qi].options.push({ option_Text: '', is_Correct: false });
  }

  removeBuilderOption(qi: number, oi: number) {
    if (this.builderQuestions[qi].options.length > 2)
      this.builderQuestions[qi].options.splice(oi, 1);
  }

  setCorrectOption(qi: number, oi: number) {
    this.builderQuestions[qi].options.forEach((o, i) => o.is_Correct = i === oi);
  }

  saveQuestions() {
    for (const q of this.builderQuestions) {
      if (!q.question_Text.trim()) { this.errorMessage = 'All questions need text.'; return; }
      if (!q.options.some(o => o.is_Correct)) { this.errorMessage = 'Mark a correct answer for each question.'; return; }
      if (q.options.some(o => !o.option_Text.trim())) { this.errorMessage = 'All options need text.'; return; }
    }
    this.savingQuestions = true;
    this.clearMessages();
    this.http.post(`${this.apiUrl}/Quizzes/${this.builderQuizId}/questions`,
      this.builderQuestions.map((q, i) => ({
        question_Text: q.question_Text,
        question_Order: i,
        options: q.options.map(o => ({ option_Text: o.option_Text, is_Correct: o.is_Correct }))
      }))
    ).subscribe({
      next: () => { this.savingQuestions = false; this.successMessage = 'Questions saved!'; this.closeBuilder(); },
      error: () => { this.savingQuestions = false; this.errorMessage = 'Failed to save questions.'; }
    });
  }

  // ─── Quiz taking ─────────────────────────────────────────────────────────────

  startQuiz(quiz: ModuleQuiz) {
    this.startingQuiz = true;
    this.takingAnswers = {};
    this.quizResult = null;
    this.clearMessages();
    this.http.post<any>(`${this.apiUrl}/Quizzes/${quiz.quiz_ID}/start`, this.userId).subscribe({
      next: (res) => {
        this.http.get<any>(`${this.apiUrl}/Quizzes/${quiz.quiz_ID}/questions`).subscribe({
          next: (data) => { this.takingQuiz = data; this.takingQuizId = quiz.quiz_ID; this.startingQuiz = false; },
          error: () => { this.startingQuiz = false; this.errorMessage = 'Failed to load quiz questions.'; }
        });
      },
      error: (err) => { this.startingQuiz = false; this.errorMessage = err?.error || 'Failed to start quiz.'; }
    });
  }

  cancelTakingQuiz() { this.takingQuizId = null; this.takingQuiz = null; this.takingAnswers = {}; }

  selectAnswer(questionId: number, optionId: number) { this.takingAnswers[questionId] = optionId; }

  submitQuizAnswers() {
    if (!this.takingQuiz) return;
    const unanswered = this.takingQuiz.questions.filter((q: any) => !this.takingAnswers[q.question_ID]);
    if (unanswered.length > 0) { this.errorMessage = `Please answer all questions (${unanswered.length} remaining).`; return; }
    this.submittingQuiz = true;
    this.clearMessages();
    this.http.post<any>(`${this.apiUrl}/Quizzes/${this.takingQuizId}/submit`, {
      student_ID: this.userId,
      answers: Object.entries(this.takingAnswers).map(([qId, oId]) => ({ question_ID: +qId, option_ID: +oId }))
    }).subscribe({
      next: (res) => {
        this.submittingQuiz = false;
        this.quizResult = res;
        this.takingQuizId = null;
        this.takingQuiz = null;
        this.successMessage = `Quiz submitted! Score: ${res.score}% (${res.correct}/${res.total} correct)`;
        this.loadQuizzes();
      },
      error: (err) => { this.submittingQuiz = false; this.errorMessage = err?.error || 'Failed to submit quiz.'; }
    });
  }

  objectKeys(obj: object): string[] { return Object.keys(obj); }

  private get authHeader(): HeadersInit {
    const token = localStorage.getItem('auth_token') ?? '';
    return { Authorization: `Bearer ${token}` };
  }

  downloadBrief(assignmentId: number, name: string) {
    fetch(`${this.apiUrl}/Assignments/${assignmentId}/download-brief`)
      .then(res => res.blob())
      .then(blob => this.triggerDownload(blob, `${name}.pdf`))
      .catch(() => this.errorMessage = 'Download failed.');
  }

  downloadSubmission(submissionId: number, filename: string) {
    fetch(`${this.apiUrl}/Assignments/submissions/${submissionId}/download`, { headers: this.authHeader })
      .then(res => res.blob())
      .then(blob => this.triggerDownload(blob, filename))
      .catch(() => this.errorMessage = 'Download failed.');
  }

  private parseUrl(raw: string): string {
    // Backend returns strings as JSON ("url") — strip surrounding quotes if present
    return raw.replace(/^"|"$/g, '').trim();
  }

  private triggerDownload(blob: Blob, filename: string) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
  Math = Math;

  get submissionsAverage(): string {
    if (this.submissions.length === 0) return '0';
    const sum = this.submissions.reduce((a: number, s: any) => a + s.quiz_Score, 0);
    return (sum / this.submissions.length).toFixed(1);
  }

  get submissionsHighest(): number {
    if (this.submissions.length === 0) return 0;
    return Math.max(...this.submissions.map((s: any) => s.quiz_Score));
  }

  // ─── Assignments ────────────────────────────────────────────────────────────

  openAssignmentForm() {
    this.assignmentName = '';
    this.assignmentDate = '';
    this.assignmentBriefFile = null;
    this.showAssignmentForm = true;
    this.clearMessages();
  }

  onBriefFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.assignmentBriefFile = input.files?.[0] ?? null;
  }

  saveAssignment() {
    if (!this.assignmentName || !this.assignmentDate) {
      this.errorMessage = 'Assignment name and due date are required.'; return;
    }
    if (this.assignmentBriefFile) {
      this.uploadingBrief = true;
      const fd = new FormData();
      fd.append('file', this.assignmentBriefFile);
      this.http.post(`${this.apiUrl}/Assignments/upload-brief`, fd, { responseType: 'text' }).subscribe({
        next: (url) => { this.uploadingBrief = false; this.doSaveAssignment(this.parseUrl(url)); },
        error: () => { this.uploadingBrief = false; this.errorMessage = 'Brief upload failed.'; }
      });
    } else {
      this.doSaveAssignment('');
    }
  }

  private doSaveAssignment(briefUrl: string) {
    this.savingAssignment = true;
    this.http.post(`${this.apiUrl}/Assignments`, {
      assignment_Name: this.assignmentName,
      assignment_Date: this.assignmentDate,
      module_Code:     this.moduleCode,
      assignment_URL:  briefUrl
    }).subscribe({
      next: () => {
        this.savingAssignment = false;
        this.successMessage = 'Assignment created!';
        this.showAssignmentForm = false;
        this.assignmentBriefFile = null;
        this.loadAssignments();
      },
      error: () => { this.savingAssignment = false; this.errorMessage = 'Failed to create assignment.'; }
    });
  }

  toggleAssignmentVisibility(a: ModuleAssignment) {
    this.togglingAssignmentVisId = a.assignment_ID;
    this.http.put(`${this.apiUrl}/Assignments/${a.assignment_ID}/visibility`,
      { is_Visible: !a.is_Visible }
    ).subscribe({
      next: () => { a.is_Visible = !a.is_Visible; this.togglingAssignmentVisId = null; },
      error: () => { this.togglingAssignmentVisId = null; }
    });
  }

  get visibleAssignments(): ModuleAssignment[] {
    return this.role === 'Student' ? this.assignments.filter(a => a.is_Visible) : this.assignments;
  }

  // ─── Assignment submissions (tutor) ─────────────────────────────────────────

  openAssignmentSubmissions(a: ModuleAssignment) {
    this.assignmentSubmissionsId = a.assignment_ID;
    this.loadingAssignmentSubmissions = true;
    this.assignmentSubmissions = [];
    this.gradingSubmissionId = null;
    this.clearMessages();
    this.http.get<any[]>(`${this.apiUrl}/Assignments/${a.assignment_ID}/submissions`).subscribe({
      next: (data) => { this.assignmentSubmissions = data; this.loadingAssignmentSubmissions = false; },
      error: () => { this.loadingAssignmentSubmissions = false; }
    });
  }

  closeAssignmentSubmissions() { this.assignmentSubmissionsId = null; this.assignmentSubmissions = []; }

  openGrade(subId: number, current: number | null, currentFeedback: string | null) {
    this.gradingSubmissionId = subId;
    this.gradeValue = current ?? null;
    this.feedbackValue = currentFeedback ?? '';
  }

  saveGrade(assignmentId: number) {
    if (!this.gradingSubmissionId) return;
    this.savingGrade = true;
    this.http.put(`${this.apiUrl}/Assignments/${assignmentId}/submissions/${this.gradingSubmissionId}/grade`,
      { grade: this.gradeValue, feedback: this.feedbackValue || null }
    ).subscribe({
      next: () => {
        this.savingGrade = false;
        const sub = this.assignmentSubmissions.find(s => s.submission_ID === this.gradingSubmissionId);
        if (sub) { sub.grade = this.gradeValue; sub.feedback = this.feedbackValue; }
        this.gradingSubmissionId = null;
        this.successMessage = 'Grade saved!';
      },
      error: () => { this.savingGrade = false; this.errorMessage = 'Failed to save grade.'; }
    });
  }

  // ─── Student submission (inline form) ───────────────────────────────────────

  openStudentSubmitForm(a: ModuleAssignment) {
    this.studentSubmitAssignmentId = a.assignment_ID;
    this.studentSubmitTitle = a.assignment_Name;
    this.studentSubmitFile = null;
    this.clearMessages();
  }

  cancelStudentSubmit() {
    this.studentSubmitAssignmentId = null;
    this.studentSubmitFile = null;
    this.studentSubmitTitle = '';
  }

  onStudentSubmitFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.studentSubmitFile = input.files?.[0] ?? null;
  }

  submitAssignment(assignmentId: number) {
    if (!this.studentSubmitFile) { this.errorMessage = 'Please select a PDF to submit.'; return; }
    if (!this.studentSubmitTitle.trim()) { this.errorMessage = 'Please enter a submission title.'; return; }
    this.submittingAssignmentId = assignmentId;
    this.clearMessages();
    const fd = new FormData();
    fd.append('studentId', String(this.userId));
    fd.append('file', this.studentSubmitFile);
    this.http.post(`${this.apiUrl}/Assignments/${assignmentId}/submit`, fd).subscribe({
      next: () => {
        this.submittingAssignmentId = null;
        this.studentSubmitAssignmentId = null;
        this.studentSubmitFile = null;
        this.studentSubmitTitle = '';
        this.successMessage = 'Assignment submitted successfully!';
        this.loadAssignments();
      },
      error: (err) => {
        this.submittingAssignmentId = null;
        this.errorMessage = err?.error || 'Submission failed.';
      }
    });
  }


  confirmDeleteAssignment(id: number) { this.deleteAssignmentId = id; this.clearMessages(); }

  deleteAssignment() {
    if (!this.deleteAssignmentId) return;
    this.http.delete(`${this.apiUrl}/Assignments/${this.deleteAssignmentId}`).subscribe({
      next: () => {
        this.successMessage = 'Assignment deleted.';
        this.deleteAssignmentId = null;
        this.loadAssignments();
      },
      error: () => { this.errorMessage = 'Failed to delete assignment.'; this.deleteAssignmentId = null; }
    });
  }

  // ─── Announcements ──────────────────────────────────────────────────────────

  loadAnnouncements() {
    this.http.get<Announcement[]>(`${this.apiUrl}/Announcements/module/${this.moduleCode}`).subscribe({
      next: (data) => { this.announcements = data; },
      error: () => {}
    });
  }

  openAnnouncementCreateForm() {
    this.announcementName = '';
    this.announcementDetails = '';
    this.announcementType = 'Update';
    this.showAnnouncementCreateForm = true;
    this.editingAnnouncement = null;
    this.clearMessages();
  }

  saveAnnouncement() {
    if (!this.announcementName) { this.errorMessage = 'Title is required.'; return; }
    this.savingAnnouncement = true;
    this.http.post(`${this.apiUrl}/Announcements`, {
      announcement_Name: this.announcementName,
      announcement_Details: this.announcementDetails,
      announcement_Type: this.announcementType,
      tutor_ID: this.role === 'Tutor' ? this.userId : undefined,
      admin_ID: this.role === 'Admin' ? this.userId : undefined,
      module_Code: this.moduleCode
    }, { responseType: 'text' }).subscribe({
      next: () => {
        this.savingAnnouncement = false;
        this.showAnnouncementCreateForm = false;
        this.loadAnnouncements();
      },
      error: () => { this.savingAnnouncement = false; this.errorMessage = 'Failed to post announcement.'; }
    });
  }

  openEditAnnouncement(a: Announcement) {
    this.showAnnouncementCreateForm = false;
    this.deleteAnnouncementId = null;
    this.editingAnnouncement = a;
    this.editAnnouncementName = a.announcement_Name;
    this.editAnnouncementDetails = a.announcement_Details;
    this.editAnnouncementType = a.announcement_Type;
    this.clearMessages();
  }

  cancelEditAnnouncement() { this.editingAnnouncement = null; }

  saveEditAnnouncement() {
    if (!this.editingAnnouncement) return;
    this.updatingAnnouncement = true;
    this.http.put(`${this.apiUrl}/Announcements/${this.editingAnnouncement.announcement_ID}`, {
      announcement_Name: this.editAnnouncementName,
      announcement_Details: this.editAnnouncementDetails,
      announcement_Type: this.editAnnouncementType,
      module_Code: this.moduleCode
    }, { responseType: 'text' }).subscribe({
      next: () => {
        this.updatingAnnouncement = false;
        this.editingAnnouncement = null;
        this.loadAnnouncements();
      },
      error: () => { this.updatingAnnouncement = false; this.errorMessage = 'Failed to update announcement.'; }
    });
  }

  confirmDeleteAnnouncement(id: number) {
    this.deleteAnnouncementId = id;
    this.editingAnnouncement = null;
    this.showAnnouncementCreateForm = false;
    this.clearMessages();
  }

  deleteAnnouncement() {
    if (!this.deleteAnnouncementId) return;
    this.deletingAnnouncement = true;
    this.http.delete(`${this.apiUrl}/Announcements/${this.deleteAnnouncementId}`, { responseType: 'text' }).subscribe({
      next: () => {
        this.deletingAnnouncement = false;
        this.deleteAnnouncementId = null;
        this.loadAnnouncements();
      },
      error: () => { this.deletingAnnouncement = false; this.errorMessage = 'Failed to delete announcement.'; }
    });
  }

  getBadgeClass(type: string): string {
    const map: Record<string, string> = {
      'Update': 'badge badge-teal',
      'Deadline': 'badge badge-warning',
      'Event': 'badge badge-info',
      'Resource': 'badge badge-purple'
    };
    return map[type] || 'badge badge-teal';
  }

  // ─── Admin: Tutor Assignment ─────────────────────────────────────────────────

  getUnassignedTutors(): UserProfile[] {
    const assignedIds = new Set(this.assignedTutors.map(t => t.tutor_ID));
    return this.allTutors.filter(t => !assignedIds.has(t.user_ID));
  }

  assignTutor() {
    if (!this.selectedTutorId) return;
    this.assigningTutor = true;
    this.http.post(`${this.apiUrl}/TutorModule/assign`, {
      tutor_ID: this.selectedTutorId,
      module_Code: this.moduleCode
    }).subscribe({
      next: () => {
        this.assigningTutor = false;
        this.successMessage = 'Tutor assigned!';
        this.selectedTutorId = null;
        this.loadAssignedTutors();
      },
      error: (err) => {
        this.assigningTutor = false;
        this.errorMessage = typeof err.error === 'string' ? err.error : 'Failed to assign tutor.';
      }
    });
  }

  unassignTutor(tutorId: number) {
    this.http.delete(`${this.apiUrl}/TutorModule/unassign/${tutorId}/${this.moduleCode}`).subscribe({
      next: () => {
        this.successMessage = 'Tutor removed from module.';
        this.loadAssignedTutors();
      },
      error: () => { this.errorMessage = 'Failed to remove tutor.'; }
    });
  }

  formatDate(d: string): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
