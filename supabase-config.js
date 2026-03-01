// ================================================
// supabase-config.js — Cấu hình Supabase dùng chung
// ================================================
// ⚠️ THAY 2 GIÁ TRỊ NÀY bằng thông tin từ Supabase project của bạn:
// Settings → API → Project URL & anon/public key
const SUPABASE_URL = "https://utyfvjuaktomcnmiqebn.supabase.co";
const SUPABASE_ANON_KEY =
  "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV0eWZ2anVha3RvbWNubWlxZWJuIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzIwMDI4NzgsImV4cCI6MjA4NzU3ODg3OH0.NU-TRMMHUOAxEHXZStg2yIhSBO-XSH2psODky5f9MxE";

// Khởi tạo client
const { createClient } = supabase;
const sb = createClient(SUPABASE_URL, SUPABASE_ANON_KEY);

// ================================================
// AUTH HELPERS
// ================================================

/** Kiểm tra đăng nhập, nếu chưa → redirect về auth.html */
async function requireAuth() {
  const {
    data: { session },
  } = await sb.auth.getSession();
  if (!session) {
    window.location.href = "auth.html";
    return null;
  }
  return session.user;
}

/** Lấy session hiện tại (không redirect) */
async function getSession() {
  const {
    data: { session },
  } = await sb.auth.getSession();
  return session;
}

/** Đăng xuất */
async function signOut() {
  await sb.auth.signOut();
  window.location.href = "auth.html";
}

// ================================================
// PROFILE HELPERS
// ================================================

/** Lấy profile của user hiện tại */
async function getProfile(userId) {
  const { data, error } = await sb
    .from("profiles")
    .select("*")
    .eq("id", userId)
    .single();
  if (error) console.error("getProfile:", error);
  return data;
}

/** Cập nhật profile */
async function updateProfile(userId, updates) {
  const { error } = await sb
    .from("profiles")
    .update({ ...updates, updated_at: new Date().toISOString() })
    .eq("id", userId);
  if (error) console.error("updateProfile:", error);
  return !error;
}

// ================================================
// EXAM / QUIZ HELPERS
// ================================================

/** Lưu kết quả thi thử */
async function saveExamResult(userId, { score, correct, total, timeSpent }) {
  const { error } = await sb.from("exam_results").insert({
    user_id: userId,
    score,
    correct,
    total,
    time_spent_seconds: timeSpent,
  });
  if (error) console.error("saveExamResult:", error);

  // Cập nhật điểm TB trong profile
  const { data: history } = await sb
    .from("exam_results")
    .select("score")
    .eq("user_id", userId);
  if (history && history.length > 0) {
    const avg =
      history.reduce((s, r) => s + parseFloat(r.score), 0) / history.length;
    await updateProfile(userId, { avg_score: avg.toFixed(2) });
  }
  return !error;
}

/** Lưu kết quả luyện tập */
async function saveQuizResult(userId, { chapter, score, correct, total }) {
  const { error } = await sb.from("quiz_results").insert({
    user_id: userId,
    chapter,
    score,
    correct,
    total,
  });
  if (error) console.error("saveQuizResult:", error);

  // Tăng số bài đã học
  const profile = await getProfile(userId);
  if (profile) {
    const newLearned = (profile.learned || 0) + total;
    const newProgress = Math.min(
      100,
      (profile.progress || 0) + Math.ceil((correct / total) * 5),
    );
    await updateProfile(userId, { learned: newLearned, progress: newProgress });
  }
  return !error;
}

/** Lấy lịch sử thi thử (10 bài gần nhất) */
async function getExamHistory(userId) {
  const { data, error } = await sb
    .from("exam_results")
    .select("*")
    .eq("user_id", userId)
    .order("created_at", { ascending: false })
    .limit(10);
  if (error) console.error("getExamHistory:", error);
  return data || [];
}

/** Lấy dữ liệu biểu đồ học tập 7 ngày gần đây */
async function getWeeklyStudyData(userId) {
  const days = [];
  for (let i = 6; i >= 0; i--) {
    const d = new Date();
    d.setDate(d.getDate() - i);
    days.push(d.toISOString().split("T")[0]);
  }
  const { data } = await sb
    .from("study_logs")
    .select("study_date, minutes")
    .eq("user_id", userId)
    .in("study_date", days);

  return days.map((d) => {
    const found = (data || []).find((r) => r.study_date === d);
    return found ? found.minutes : 0;
  });
}

/** Ghi nhận thời gian học hôm nay */
async function logStudyTime(userId, minutes) {
  const today = new Date().toISOString().split("T")[0];
  const { data: existing } = await sb
    .from("study_logs")
    .select("id, minutes")
    .eq("user_id", userId)
    .eq("study_date", today)
    .single();

  if (existing) {
    await sb
      .from("study_logs")
      .update({ minutes: existing.minutes + minutes })
      .eq("id", existing.id);
  } else {
    await sb
      .from("study_logs")
      .insert({ user_id: userId, study_date: today, minutes });
  }

  // Cập nhật total_study_minutes trong profile
  const profile = await getProfile(userId);
  if (profile) {
    await updateProfile(userId, {
      total_study_minutes: (profile.total_study_minutes || 0) + minutes,
    });
  }
}

/** Cập nhật streak (gọi khi user học) */
async function updateStreak(userId) {
  const profile = await getProfile(userId);
  if (!profile) return;

  const today = new Date().toISOString().split("T")[0];
  const yesterday = new Date(Date.now() - 86400000).toISOString().split("T")[0];
  const lastDate = profile.last_study_date;

  let newStreak = profile.streak || 0;
  if (lastDate === yesterday) {
    newStreak += 1; // Tiếp tục streak
  } else if (lastDate !== today) {
    newStreak = 1; // Reset streak
  }

  await updateProfile(userId, {
    streak: newStreak,
    last_study_date: today,
  });
  return newStreak;
}

// ================================================
// FORMAT HELPERS
// ================================================
function formatDate(isoStr) {
  if (!isoStr) return "";
  return new Date(isoStr).toLocaleDateString("vi-VN");
}

function formatTime(seconds) {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}p ${s}s`;
}
