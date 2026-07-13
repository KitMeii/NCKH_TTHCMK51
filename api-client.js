/**
 * api-client.js — thay thế supabase-config.js.
 *
 * Toàn bộ gọi Supabase/Groq trực tiếp từ browser đã bị loại bỏ. Mọi request giờ đi qua Gateway
 * (YARP), Gateway xác thực JWT rồi forward xuống từng microservice. Xem kế hoạch di trú tại
 * README của repo / lịch sử commit "Add YARP gateway" v.v.
 *
 * Cấu hình domain Gateway: đặt `window.TTHCM_API_BASE = "https://api.tenmien.vn"` trong 1 thẻ
 * <script> TRƯỚC khi load file này (ví dụ ở <head> mỗi trang, hoặc 1 file config riêng khi
 * deploy production). Nếu không đặt, mặc định trỏ về Gateway chạy local lúc dev
 * (http://localhost:8080).
 */

const API_BASE = window.TTHCM_API_BASE || "http://localhost:8080";

const STORAGE_KEYS = {
  token: "tthcm_access_token",
  expiresAt: "tthcm_token_expires_at",
  user: "tthcm_user",
};

// ---------------------------------------------------------------------------
// Token / session storage
// ---------------------------------------------------------------------------

function getToken() {
  return localStorage.getItem(STORAGE_KEYS.token);
}

function getStoredUser() {
  const raw = localStorage.getItem(STORAGE_KEYS.user);
  return raw ? JSON.parse(raw) : null;
}

function isTokenExpired() {
  const expiresAt = localStorage.getItem(STORAGE_KEYS.expiresAt);
  if (!expiresAt) return true;
  return new Date(expiresAt).getTime() <= Date.now();
}

function isAuthenticated() {
  return !!getToken() && !isTokenExpired();
}

function storeSession(authResponse) {
  localStorage.setItem(STORAGE_KEYS.token, authResponse.accessToken);
  localStorage.setItem(STORAGE_KEYS.expiresAt, authResponse.expiresAtUtc);
  localStorage.setItem(STORAGE_KEYS.user, JSON.stringify(authResponse.user));
}

function clearSession() {
  localStorage.removeItem(STORAGE_KEYS.token);
  localStorage.removeItem(STORAGE_KEYS.expiresAt);
  localStorage.removeItem(STORAGE_KEYS.user);
}

/** Call at the top of every protected page. Redirects to auth.html if not logged in. */
function requireAuth() {
  if (!isAuthenticated()) {
    window.location.href = "auth.html";
    return false;
  }
  return true;
}

/** Non-redirecting session check — returns {token, user} or null. */
function getSession() {
  if (!isAuthenticated()) return null;
  return { token: getToken(), user: getStoredUser() };
}

function signOut() {
  clearSession();
  window.location.href = "auth.html";
}

// ---------------------------------------------------------------------------
// Core fetch wrapper — every service responds with { success, data, error }
// ---------------------------------------------------------------------------

class ApiError extends Error {
  constructor(code, message, status) {
    super(message);
    this.code = code;
    this.status = status;
  }
}

async function apiFetch(path, { method = "GET", body, auth = true } = {}) {
  const headers = { "Content-Type": "application/json" };

  if (auth) {
    const token = getToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }

  let response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch (networkError) {
    throw new ApiError("NETWORK_ERROR", "Không thể kết nối máy chủ. Kiểm tra lại kết nối mạng.", 0);
  }

  if (response.status === 401) {
    clearSession();
    window.location.href = "auth.html";
    throw new ApiError("UNAUTHORIZED", "Phiên đăng nhập đã hết hạn.", 401);
  }

  // 204 No Content / empty body
  const text = await response.text();
  const json = text ? JSON.parse(text) : { success: response.ok, data: null };

  if (!response.ok || json.success === false) {
    const error = json.error || {};
    throw new ApiError(error.code || "UNKNOWN_ERROR", error.message || "Đã xảy ra lỗi.", response.status);
  }

  return json.data;
}

// ---------------------------------------------------------------------------
// Auth
// ---------------------------------------------------------------------------

async function login(email, password) {
  const auth = await apiFetch("/api/v1/auth/login", { method: "POST", body: { email, password }, auth: false });
  storeSession(auth);
  return auth.user;
}

async function register(email, password, name) {
  const auth = await apiFetch("/api/v1/auth/register", { method: "POST", body: { email, password, name }, auth: false });
  storeSession(auth);
  return auth.user;
}

async function getProfile() {
  const user = await apiFetch("/api/v1/auth/me");
  localStorage.setItem(STORAGE_KEYS.user, JSON.stringify(user));
  return user;
}

async function updateProfile({ name, course, className }) {
  const user = await apiFetch("/api/v1/auth/me", { method: "PUT", body: { name, course, className } });
  localStorage.setItem(STORAGE_KEYS.user, JSON.stringify(user));
  return user;
}

// ---------------------------------------------------------------------------
// Quiz (trắc nghiệm)
// ---------------------------------------------------------------------------

function getPracticeQuestions(chapter) {
  const qs = chapter ? `?chapter=${encodeURIComponent(chapter)}` : "";
  return apiFetch(`/api/v1/quiz/questions/practice${qs}`);
}

function submitPracticeQuiz(chapter, answers) {
  return apiFetch("/api/v1/quiz/practice/submit", { method: "POST", body: { chapter, answers } });
}

function submitExam(answers, timeSpentSeconds) {
  return apiFetch("/api/v1/quiz/exams/submit", { method: "POST", body: { answers, timeSpentSeconds } });
}

function getWrongAnswers() {
  return apiFetch("/api/v1/quiz/wrong-answers");
}

function getMyQuizResults() {
  return apiFetch("/api/v1/quiz/my-results");
}

// vấn đáp (oral)
function getOralQuestions(chapter) {
  const qs = chapter ? `?chapter=${encodeURIComponent(chapter)}` : "";
  return apiFetch(`/api/v1/quiz/oral-questions/practice${qs}`);
}

function submitOralAnswer(questionId, mainAnswer, followupAnswers) {
  return apiFetch("/api/v1/quiz/oral/submit", { method: "POST", body: { questionId, mainAnswer, followupAnswers } });
}

function getMyOralResults() {
  return apiFetch("/api/v1/quiz/oral/results");
}

// Teacher/Admin: ngân hàng câu hỏi
function listQuestionsBank(chapter) {
  const qs = chapter ? `?chapter=${encodeURIComponent(chapter)}` : "";
  return apiFetch(`/api/v1/quiz/questions${qs}`);
}
function createQuestion(question) {
  return apiFetch("/api/v1/quiz/questions", { method: "POST", body: question });
}
function updateQuestion(id, question) {
  return apiFetch(`/api/v1/quiz/questions/${id}`, { method: "PUT", body: question });
}
function deleteQuestion(id) {
  return apiFetch(`/api/v1/quiz/questions/${id}`, { method: "DELETE" });
}

function listOralQuestionsBank(chapter) {
  const qs = chapter ? `?chapter=${encodeURIComponent(chapter)}` : "";
  return apiFetch(`/api/v1/quiz/oral-questions${qs}`);
}
function createOralQuestion(question) {
  return apiFetch("/api/v1/quiz/oral-questions", { method: "POST", body: question });
}
function updateOralQuestion(id, question) {
  return apiFetch(`/api/v1/quiz/oral-questions/${id}`, { method: "PUT", body: question });
}
function deleteOralQuestion(id) {
  return apiFetch(`/api/v1/quiz/oral-questions/${id}`, { method: "DELETE" });
}

// ---------------------------------------------------------------------------
// Content (tài liệu / bài giảng)
// ---------------------------------------------------------------------------

function listMaterials(chapter) {
  const qs = chapter ? `?chapter=${encodeURIComponent(chapter)}` : "";
  return apiFetch(`/api/v1/content/materials${qs}`);
}
function getMaterial(id) {
  return apiFetch(`/api/v1/content/materials/${id}`);
}
function createMaterial(material) {
  return apiFetch("/api/v1/content/materials", { method: "POST", body: material });
}
function updateMaterial(id, material) {
  return apiFetch(`/api/v1/content/materials/${id}`, { method: "PUT", body: material });
}
function deleteMaterial(id) {
  return apiFetch(`/api/v1/content/materials/${id}`, { method: "DELETE" });
}
function incrementMaterialView(id) {
  return apiFetch(`/api/v1/content/materials/${id}/view`, { method: "POST" });
}

// ---------------------------------------------------------------------------
// Progress (tiến độ / bảng xếp hạng)
// ---------------------------------------------------------------------------

function logStudyTime(minutes) {
  return apiFetch("/api/v1/progress/study-logs", { method: "POST", body: { minutes } });
}
function getWeeklyStudyData() {
  return apiFetch("/api/v1/progress/study-logs/weekly");
}
function getMyProgress() {
  return apiFetch("/api/v1/progress/me");
}
function getLeaderboard(top = 30) {
  return apiFetch(`/api/v1/progress/leaderboard?top=${top}`);
}

// ---------------------------------------------------------------------------
// AI (chatbot / giảng viên ảo / chấm vấn đáp / trích xuất câu hỏi)
// ---------------------------------------------------------------------------

function chatWithAI(messages) {
  return apiFetch("/api/v1/ai/chat", { method: "POST", body: { messages } });
}

/** Tiện ích gọi chatbot với 1 prompt đơn (dùng cho các tác vụ AI phụ, không có endpoint
 * riêng — vd tóm tắt giọng nói, sinh câu hỏi phụ trong vấn đáp — KHÔNG dùng cho việc chấm
 * điểm chính thức, việc đó luôn phải qua endpoint chuyên biệt như /grade-oral. */
async function askAI(prompt) {
  const result = await chatWithAI([{ role: "user", content: prompt }]);
  return result.reply;
}
function generateLecture(chapter, topic, sourceText) {
  return apiFetch("/api/v1/ai/generate-lecture", { method: "POST", body: { chapter, topic, sourceText } });
}
function generateComprehensionQuestions(chapter, sourceText) {
  return apiFetch("/api/v1/ai/generate-comprehension-questions", { method: "POST", body: { chapter, sourceText } });
}
function extractQuestionsFromDocument(chapter, sourceText, count = 10) {
  return apiFetch("/api/v1/ai/extract-questions", { method: "POST", body: { chapter, sourceText, count } });
}

// ---------------------------------------------------------------------------
// Admin
// ---------------------------------------------------------------------------

function adminListUsers(role) {
  const qs = role ? `?role=${encodeURIComponent(role)}` : "";
  return apiFetch(`/api/v1/admin/users${qs}`);
}
function adminChangeRole(userId, role) {
  return apiFetch(`/api/v1/admin/users/${userId}/role`, { method: "PUT", body: { role } });
}
function adminAuditLog(top = 50) {
  return apiFetch(`/api/v1/admin/audit-log?top=${top}`);
}
function adminGetConfig() {
  return apiFetch("/api/v1/admin/config");
}
function adminSetConfig(key, value) {
  return apiFetch(`/api/v1/admin/config/${encodeURIComponent(key)}`, { method: "PUT", body: { value } });
}
function adminStatsOverview() {
  return apiFetch("/api/v1/admin/stats/overview");
}

// ---------------------------------------------------------------------------
// Format helpers (pure JS, no API calls)
// ---------------------------------------------------------------------------

function formatDate(isoStr) {
  if (!isoStr) return "";
  return new Date(isoStr).toLocaleDateString("vi-VN");
}

function formatTime(seconds) {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}p ${s}s`;
}
