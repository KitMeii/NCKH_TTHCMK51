// ============================================================
// DÙNG CHUNG giữa teacher/quan-ly-noi-dung.html và admin/quan-tri-he-thong.html
// Sửa file này thì cả 2 trang đều nhận thay đổi (không cần sửa 2 nơi).
// Ngược lại, các khối <div class="panel">...</div> trong 2 file HTML là
// bản sao vật lý (không có build step để include HTML dùng chung) — mỗi
// khối HTML dùng chung có comment "DÙNG CHUNG với ..." nhắc sửa cả 2 bên.
//
// Yêu cầu ở trang gọi file này: đã load api-client.js trước, có sẵn các
// element #toast, #questionsList, #oralList, #matList, #ov-*, #q*, #batch*,
// #xlsx*, #word*, #pasteArea/#pasteChapter/#pasteStatus, #oralImportStatus,
// #oralModal + #oral-chapter/#oral-q/#oral-a/#oral-diff, #matTitle/#matChapter/
// #matDesc/#matFileInput/#matProg*/#matUploadStatus. Trang gọi file này phải tự
// khai báo `let me` (session user) trước khi các hàm dưới đây chạy, và tự gọi
// init() + document.addEventListener("DOMContentLoaded", init) ở script riêng.
// ============================================================

pdfjsLib.GlobalWorkerOptions.workerSrc =
  "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js";

let allQuestions = [],
  allOralQ = [],
  allMaterials = [];

// ============================================================
// PANEL SWITCHING — generic: các panel role-riêng (VD "students", "settings"
// bản đầy đủ) chỉ tồn tại ở admin/quan-tri-he-thong.html; ở
// teacher/quan-ly-noi-dung.html các hàm loadStudents/loadAuditLog/
// loadSystemConfig không được định nghĩa nên các nhánh if dưới đây tự
// bỏ qua (typeof === "function" guard), không lỗi.
// ============================================================
function showPanel(name) {
  document
    .querySelectorAll(".panel")
    .forEach((p) => p.classList.remove("active"));
  document
    .querySelectorAll(".sidebar-item")
    .forEach((i) => i.classList.remove("active"));
  document.getElementById("panel-" + name).classList.add("active");
  event?.target?.closest(".sidebar-item")?.classList.add("active");
  if (name === "materials") loadMaterials();
  if (name === "overview") loadOverview();
  if (name === "questions") loadQuestions();
  if (name === "students" && typeof loadStudents === "function" && me.role === "Admin")
    loadStudents();
  if (name === "oral") loadOralQuestions();
  if (name === "settings" && typeof loadAuditLog === "function" && me.role === "Admin") {
    loadAuditLog();
    loadSystemConfig();
  }
}

// ============================================================
// OVERVIEW — Admin thấy số liệu toàn hệ thống thật (admin-service);
// Teacher thấy số đếm gần đúng từ chính dữ liệu mình tải được.
// ============================================================
async function loadOverview() {
  if (me.role === "Admin") {
    try {
      const stats = await adminStatsOverview();
      document.getElementById("ov-students").textContent = stats.totalStudents;
      document.getElementById("ov-questions").textContent = stats.totalQuestions;
      document.getElementById("ov-oral").textContent = stats.totalOralQuestions;
      document.getElementById("ov-materials").textContent = stats.totalMaterials;
      document.getElementById("ov-note").textContent =
        `${stats.totalTeachers} giảng viên · ${stats.totalAdmins} admin`;
      return;
    } catch (err) {
      document.getElementById("ov-note").textContent =
        "Không tải được thống kê hệ thống: " + err.message;
    }
  } else {
    document.getElementById("ov-students").textContent = "—";
    document.getElementById("ov-note").textContent =
      "Đăng nhập với vai trò Admin để xem thống kê toàn hệ thống (số học viên, giảng viên...).";
  }
  // Fallback: đếm từ dữ liệu ngân hàng câu hỏi/tài liệu của chính giảng viên đã tải
  try {
    const [qs, oqs, mats] = await Promise.all([
      listQuestionsBank(),
      listOralQuestionsBank(),
      listMaterials(),
    ]);
    document.getElementById("ov-questions").textContent = qs.length;
    document.getElementById("ov-oral").textContent = oqs.length;
    document.getElementById("ov-materials").textContent = mats.length;
  } catch {}
}

// ============================================================
// QUESTIONS (trắc nghiệm) — quiz-service question bank
// ============================================================
async function loadQuestions() {
  try {
    allQuestions = await listQuestionsBank();
  } catch (err) {
    allQuestions = [];
    showToast("Lỗi tải câu hỏi: " + err.message, "error");
  }
  const chapters = [
    ...new Set(allQuestions.map((q) => q.chapter).filter(Boolean)),
  ];
  const sel = document.getElementById("qChapter");
  sel.innerHTML =
    '<option value="">Tất cả chương</option>' +
    chapters.map((c) => `<option value="${c}">${c}</option>`).join("");
  renderQuestions(allQuestions);
}

function renderQuestions(list) {
  document.getElementById("qCount").textContent = `${list.length} câu hỏi`;
  const el = document.getElementById("questionsList");
  if (!list.length) {
    el.innerHTML =
      '<div class="empty"><i class="fas fa-inbox"></i><br>Chưa có câu hỏi. Nhấn Import để thêm.</div>';
    return;
  }
  el.innerHTML =
    list
      .slice(0, 50)
      .map((q, i) => {
        const opts = [q.optionA, q.optionB, q.optionC, q.optionD].filter(Boolean);
        const letters = ["A", "B", "C", "D"];
        return `<div class="q-card">
      <div style="display:flex;justify-content:space-between;align-items:flex-start;gap:8px;">
        <div style="flex:1;">
          <div class="q-chapter">${q.chapter || "Chung"}</div>
          <div class="q-text">${i + 1}. ${q.questionText}</div>
          <div class="q-opts">${opts.map((o, j) => `<span class="q-opt ${j === q.correctAnswer ? "correct" : ""}">${letters[j]}. ${o}</span>`).join("")}</div>
          ${q.explanation ? `<div style="font-size:0.72rem;color:var(--gray-500);margin-top:4px;">💡 ${q.explanation}</div>` : ""}
        </div>
        <button onclick="deleteQuestionRow('${q.id}')" style="background:none;border:none;cursor:pointer;color:var(--gray-400);padding:4px;" title="Xóa" aria-label="Xóa câu hỏi"><i class="fas fa-trash"></i></button>
      </div>
    </div>`;
      })
      .join("") +
    (list.length > 50
      ? `<div style="grid-column:1/-1;text-align:center;padding:12px;font-size:0.78rem;color:var(--gray-400);">Hiển thị 50/${list.length} câu. Import thêm để bổ sung.</div>`
      : "");
}

function filterQuestions() {
  const q = document.getElementById("qSearch").value.toLowerCase();
  const ch = document.getElementById("qChapter").value;
  renderQuestions(
    allQuestions.filter(
      (x) =>
        (!q || x.questionText.toLowerCase().includes(q)) && (!ch || x.chapter === ch),
    ),
  );
}

async function deleteQuestionRow(id) {
  if (!confirm("Xóa câu hỏi này?")) return;
  try {
    await deleteQuestion(id);
    showToast("Đã xóa câu hỏi", "success");
  } catch (err) {
    showToast("Lỗi: " + err.message, "error");
  }
  loadQuestions();
}

// ============================================================
// ORAL QUESTIONS
// ============================================================
async function loadOralQuestions() {
  try {
    allOralQ = await listOralQuestionsBank();
  } catch (err) {
    allOralQ = [];
    showToast("Lỗi tải câu hỏi vấn đáp: " + err.message, "error");
  }
  const el = document.getElementById("oralList");
  if (!allOralQ.length) {
    el.innerHTML =
      '<div class="empty"><i class="fas fa-comments"></i><br>Chưa có câu hỏi vấn đáp</div>';
    return;
  }
  el.innerHTML = allOralQ
    .map(
      (q, i) => `
    <div class="q-card">
      <div style="display:flex;justify-content:space-between;align-items:flex-start;gap:8px;">
        <div style="flex:1;">
          <div class="q-chapter">${q.chapter || "Chung"} · Độ khó: ${"⭐".repeat(q.difficulty || 1)}</div>
          <div class="q-text">${i + 1}. ${q.questionText}</div>
          <div style="font-size:0.75rem;color:var(--gray-600);margin-top:6px;background:var(--gray-50);padding:8px;border-radius:6px;"><strong>Đáp án chuẩn:</strong> ${q.expectedAnswer || "—"}</div>
        </div>
        <button onclick="deleteOralQ('${q.id}')" style="background:none;border:none;cursor:pointer;color:var(--gray-400);padding:4px;" aria-label="Xóa câu hỏi vấn đáp"><i class="fas fa-trash"></i></button>
      </div>
    </div>`,
    )
    .join("");
}

function openAddOralModal() {
  openModal("oralModal");
}

async function saveOralQuestion() {
  const chapter = document.getElementById("oral-chapter").value.trim();
  const questionText = document.getElementById("oral-q").value.trim();
  const expectedAnswer = document.getElementById("oral-a").value.trim();
  const difficulty = parseInt(document.getElementById("oral-diff").value);
  if (!chapter || !questionText || !expectedAnswer)
    return alert("Vui lòng điền đủ thông tin!");
  try {
    await createOralQuestion({ chapter, questionText, expectedAnswer, difficulty });
  } catch (err) {
    showToast("Lỗi: " + err.message, "error");
    return;
  }
  showToast("Đã thêm câu hỏi vấn đáp!", "success");
  closeModal("oralModal");
  document.getElementById("oral-chapter").value = "";
  document.getElementById("oral-q").value = "";
  document.getElementById("oral-a").value = "";
  loadOralQuestions();
}

async function deleteOralQ(id) {
  if (!confirm("Xóa câu hỏi vấn đáp này?")) return;
  try {
    await deleteOralQuestion(id);
    showToast("Đã xóa", "success");
  } catch (err) {
    showToast("Lỗi: " + err.message, "error");
  }
  loadOralQuestions();
}

// ============================================================
// IMPORT EXCEL (questions)
// ============================================================
async function importExcel(e) {
  const file = e.target.files[0];
  if (!file) return;
  const prog = document.getElementById("xlsxProg");
  const fill = document.getElementById("xlsxProgFill");
  const status = document.getElementById("xlsxStatus");
  prog.style.display = "block";
  fill.style.width = "10%";
  status.textContent = "Đang đọc file...";

  const buffer = await file.arrayBuffer();
  const wb = XLSX.read(buffer);
  const wsName = wb.SheetNames.find((n) => n.includes("CÂU HỎI")) || wb.SheetNames[0];
  const ws = wb.Sheets[wsName];
  const rows = XLSX.utils.sheet_to_json(ws, { defval: "" });

  fill.style.width = "40%";
  status.textContent = `Tìm thấy ${rows.length} dòng, đang xử lý...`;

  const questions = rows
    .filter((r) => r.cau_hoi || r["Câu hỏi"])
    .map((r) => ({
      chapter: r.chuong || r["Tên chương"] || "Chương mới",
      questionText: r.cau_hoi || r["Câu hỏi"],
      optionA: r.lua_chon_a || r["A. Lựa chọn A"] || "",
      optionB: r.lua_chon_b || r["B. Lựa chọn B"] || "",
      optionC: r.lua_chon_c || r["C. Lựa chọn C"] || "",
      optionD: r.lua_chon_d || r["D. Lựa chọn D"] || "",
      correctAnswer: parseInt(r.dap_an_dung ?? r["Đáp án (0/1/2/3)"] ?? 0) || 0,
      explanation: r.giai_thich || r["Giải thích"] || "",
    }))
    .filter((q) => q.questionText);

  fill.style.width = "70%";
  const inserted = await batchInsertQuestions(questions, (done, total) => {
    fill.style.width = 70 + (done / total) * 30 + "%";
  });

  fill.style.width = "100%";
  status.textContent = `✅ Đã import ${inserted}/${questions.length} câu hỏi thành công!`;
  status.style.color = "#2e7d32";
  showToast(`Import thành công ${inserted} câu!`, "success");
  loadQuestions();
  loadOverview();
}

async function importOralExcel(e) {
  const file = e.target.files[0];
  if (!file) return;
  const buffer = await file.arrayBuffer();
  const wb = XLSX.read(buffer);
  const wsName = wb.SheetNames.find((n) => n.includes("VẤN ĐÁP")) || wb.SheetNames[0];
  const ws = wb.Sheets[wsName];
  const rows = XLSX.utils.sheet_to_json(ws, { defval: "" });

  const oqs = rows
    .filter((r) => r.cau_hoi || r["Câu hỏi vấn đáp"])
    .map((r) => ({
      chapter: r.chuong || r["Chương/Phần"] || "Chung",
      questionText: r.cau_hoi || r["Câu hỏi vấn đáp"],
      expectedAnswer: r.dap_an_chuan || r["Đáp án chuẩn"] || "",
      difficulty: parseInt(r.do_kho || r["Độ khó (1/2/3)"] || 2),
    }))
    .filter((q) => q.questionText);

  const status = document.getElementById("oralImportStatus");
  const inserted = await batchInsertOral(oqs);
  status.textContent = `✅ Đã import ${inserted}/${oqs.length} câu hỏi vấn đáp!`;
  status.style.color = "#2e7d32";
  showToast(`Import ${inserted} câu vấn đáp!`, "success");
  loadOralQuestions();
}

// ============================================================
// IMPORT WORD
// ============================================================
async function importWord(e) {
  const file = e.target.files[0];
  if (!file) return;
  const chapter = document.getElementById("wordChapter").value.trim() || "Chương mới";
  const status = document.getElementById("wordStatus");
  status.textContent = "Đang đọc file Word...";

  const buffer = await file.arrayBuffer();
  const result = await mammoth.extractRawText({ arrayBuffer: buffer });
  const text = result.value;
  const parsed = parseQText(text, chapter);

  if (!parsed.length) {
    status.textContent = "❌ Không tìm được câu hỏi. Kiểm tra định dạng!";
    return;
  }
  status.textContent = `Tìm thấy ${parsed.length} câu, đang lưu...`;

  const inserted = await batchInsertQuestions(parsed);
  status.textContent = `✅ Import thành công ${inserted}/${parsed.length} câu hỏi vào "${chapter}"!`;
  status.style.color = "#2e7d32";
  showToast(`Import ${inserted} câu!`, "success");
  loadQuestions();
}

async function importPaste() {
  const text = document.getElementById("pasteArea").value.trim();
  const chapter = document.getElementById("pasteChapter").value.trim() || "Chương mới";
  const status = document.getElementById("pasteStatus");
  if (!text) return alert("Vui lòng dán nội dung câu hỏi!");

  const parsed = parseQText(text, chapter);
  if (!parsed.length) {
    status.textContent = "❌ Không tìm được câu hỏi. Kiểm tra định dạng!";
    return;
  }
  const inserted = await batchInsertQuestions(parsed);
  status.textContent = `✅ Import thành công ${inserted}/${parsed.length} câu hỏi vào "${chapter}"!`;
  status.style.color = "#2e7d32";
  document.getElementById("pasteArea").value = "";
  showToast(`Import ${inserted} câu!`, "success");
  loadQuestions();
}

/** Parse văn bản dạng "Câu X: ... A. ... B. ... ĐÁP ÁN: A" → CreateQuestionRequest[] */
function parseQText(text, chapter) {
  const results = [];
  text = text.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
  const blocks = text.split(/(?=Câu\s*\d+\s*[:\.]\s*)/i).filter((b) => b.trim());
  for (const block of blocks) {
    const lines = block
      .split("\n")
      .map((l) => l.trim())
      .filter(Boolean);
    if (lines.length < 3) continue;
    const qText = lines[0].replace(/^Câu\s*\d+\s*[:\.]\s*/i, "").trim();
    if (!qText) continue;
    const opts = [];
    for (const line of lines.slice(1)) {
      const m = line.match(/^[ABCDabcd]\s*[\.:\)]\s*(.+)/);
      if (m) opts.push(m[1].trim());
      if (opts.length === 4) break;
    }
    if (opts.length < 2) continue;
    let ans = 0;
    const ansLine = lines.find((l) => /đáp án|answer/i.test(l));
    if (ansLine) {
      const m = ansLine.match(/[ABCD]/i);
      if (m) ans = ["A", "B", "C", "D"].indexOf(m[0].toUpperCase());
    }
    const expLine = lines.find((l) => /giải thích|note/i.test(l));
    const exp = expLine ? expLine.replace(/^(giải thích|note)\s*[:\.]\s*/i, "") : "";
    results.push({
      chapter,
      questionText: qText,
      optionA: opts[0] || "",
      optionB: opts[1] || "",
      optionC: opts[2] || "",
      optionD: opts[3] || "",
      correctAnswer: Math.max(0, ans),
      explanation: exp,
    });
  }
  return results;
}

// ============================================================
// HELPERS
// ============================================================
function showToast(msg, type = "") {
  const t = document.getElementById("toast");
  t.textContent = msg;
  t.className = `toast ${type} show`;
  setTimeout(() => t.classList.remove("show"), 3000);
}
function openModal(id) {
  const el = document.getElementById(id);
  el.classList.add("show");
  el.setAttribute("aria-hidden", "false");
}
function closeModal(id) {
  const el = document.getElementById(id);
  el.classList.remove("show");
  el.setAttribute("aria-hidden", "true");
}

// ════════════════════════════════════════════
// BATCH IMPORT — PDF + Word + Excel + Text
// ════════════════════════════════════════════

let batchFiles = []; // [{file, id, status, count}]
let batchRunning = false;

function addBatchFiles(files) {
  for (const f of files) {
    const ext = f.name.split(".").pop().toLowerCase();
    if (!["pdf", "docx", "xlsx", "xls", "txt"].includes(ext)) continue;
    if (batchFiles.find((b) => b.file.name === f.name && b.file.size === f.size)) continue;
    batchFiles.push({
      file: f,
      id: Date.now() + Math.random(),
      status: "pending",
      count: 0,
      ext,
    });
  }
  renderBatchList();
}

function renderBatchList() {
  const el = document.getElementById("batchFileList");
  const startBtn = document.getElementById("batchStartBtn");
  const clearBtn = document.getElementById("batchClearBtn");
  const hint = document.getElementById("batchHint");

  if (!batchFiles.length) {
    el.innerHTML = "";
    startBtn.style.display = "none";
    clearBtn.style.display = "none";
    hint.textContent = "Chọn file để bắt đầu";
    return;
  }

  el.innerHTML = batchFiles
    .map((b, i) => {
      const icons = { pdf: "📄", docx: "📝", xlsx: "📊", xls: "📊", txt: "📃" };
      const colors = { pdf: "#c62828", docx: "#1565c0", xlsx: "#2e7d32", xls: "#2e7d32", txt: "#555" };
      const statusMap = {
        pending: { cls: "bfs-pending", text: "⏳ Chờ" },
        loading: { cls: "bfs-loading", text: "🔄 Đang xử lý..." },
        done: { cls: "bfs-done", text: b.count ? `✅ +${b.count} câu` : "✅ Xong" },
        error: { cls: "bfs-error", text: "❌ Lỗi" },
      };
      const s = statusMap[b.status] || statusMap.pending;
      const size = (b.file.size / 1024).toFixed(0) + " KB";
      return `<div class="batch-file-item" id="bfi_${i}">
      <div class="batch-file-icon" style="color:${colors[b.ext]}">${icons[b.ext]}</div>
      <div class="batch-file-info">
        <div class="batch-file-name">${b.file.name}</div>
        <div class="batch-file-meta">${b.ext.toUpperCase()} · ${size}</div>
      </div>
      <div class="batch-file-status ${s.cls}">${s.text}</div>
    </div>`;
    })
    .join("");

  startBtn.style.display = batchRunning ? "none" : "flex";
  clearBtn.style.display = "flex";
  hint.textContent = `${batchFiles.length} file đã chọn`;
}

function clearBatchFiles() {
  if (batchRunning) return;
  batchFiles = [];
  renderBatchList();
  document.getElementById("batchOverall").style.display = "none";
  document.getElementById("batchFileInput").value = "";
}

async function startBatchImport() {
  if (!batchFiles.length || batchRunning) return;
  batchRunning = true;
  document.getElementById("batchStartBtn").style.display = "none";
  document.getElementById("batchOverall").style.display = "block";

  let totalImported = 0;
  let totalErrors = 0;

  for (let i = 0; i < batchFiles.length; i++) {
    const b = batchFiles[i];
    b.status = "loading";
    renderBatchList();
    updateBatchProgress(i, batchFiles.length, `Đang xử lý: ${b.file.name}`);

    try {
      let count = 0;
      if (b.ext === "pdf") {
        count = await importPDFBatch(b.file);
      } else if (b.ext === "docx") {
        count = await importWordBatch(b.file);
      } else if (b.ext === "xlsx" || b.ext === "xls") {
        count = await importExcelBatch(b.file);
      } else if (b.ext === "txt") {
        count = await importTextBatch(b.file);
      }
      b.status = "done";
      b.count = count;
      totalImported += count;
    } catch (err) {
      b.status = "error";
      b.errorMsg = err.message;
      totalErrors++;
      console.error("Batch import error:", b.file.name, err);
    }
    renderBatchList();
  }

  updateBatchProgress(batchFiles.length, batchFiles.length, "Hoàn thành!");
  document.getElementById("batchSummary").textContent =
    `✅ Đã import ${totalImported} câu hỏi từ ${batchFiles.length - totalErrors} file` +
    (totalErrors ? ` · ❌ ${totalErrors} file lỗi` : "");
  batchRunning = false;
  document.getElementById("batchStartBtn").style.display = "flex";
  loadQuestions();
  showToast(`Import xong! +${totalImported} câu hỏi`, "success");
}

function updateBatchProgress(done, total, text) {
  const pct = total ? Math.round((done / total) * 100) : 0;
  document.getElementById("batchOverallFill").style.width = pct + "%";
  document.getElementById("batchOverallText").textContent = text + ` (${done}/${total})`;
}

// ── PDF IMPORT ──
async function importPDFBatch(file) {
  const buffer = await file.arrayBuffer();
  const pdf = await pdfjsLib.getDocument({ data: buffer }).promise;

  let fullText = "";
  for (let p = 1; p <= Math.min(pdf.numPages, 60); p++) {
    const page = await pdf.getPage(p);
    const tc = await page.getTextContent();
    fullText += tc.items.map((i) => i.str).join(" ") + "\n";
  }

  const chapterName = file.name.replace(/\.pdf$/i, "").replace(/_/g, " ").trim();

  // Thử tách câu hỏi trực tiếp từ text trước, nếu không đủ thì nhờ AI (ai-service) trích xuất
  let parsed = parseQText(fullText, chapterName);

  if (parsed.length < 3) {
    parsed = await aiExtractQuestions(fullText, chapterName);
  }

  if (!parsed.length) return 0;
  return await batchInsertQuestions(parsed);
}

/** Trích xuất câu hỏi từ nội dung bài giảng qua ai-service (Teacher/Admin only) —
 * không còn gọi Groq trực tiếp từ browser. */
async function aiExtractQuestions(text, chapterName) {
  try {
    const result = await extractQuestionsFromDocument(chapterName, text.slice(0, 6000), 12);
    return (result.questions || []).map((q) => ({
      chapter: chapterName,
      questionText: q.questionText,
      optionA: q.optionA,
      optionB: q.optionB,
      optionC: q.optionC || "",
      optionD: q.optionD || "",
      correctAnswer: q.correctAnswer || 0,
      explanation: q.explanation || "",
    }));
  } catch (e) {
    console.error("AI extract failed:", e);
    return [];
  }
}

// ── WORD BATCH ──
async function importWordBatch(file) {
  const buffer = await file.arrayBuffer();
  const result = await mammoth.extractRawText({ arrayBuffer: buffer });
  const text = result.value;
  const chapterName = file.name.replace(/\.docx$/i, "").replace(/_/g, " ").trim();
  const parsed = parseQText(text, chapterName);
  if (!parsed.length) return 0;
  return await batchInsertQuestions(parsed);
}

// ── EXCEL BATCH ──
async function importExcelBatch(file) {
  const buffer = await file.arrayBuffer();
  const wb = XLSX.read(buffer, { type: "array" });

  const sheetName =
    wb.SheetNames.find((n) => n.includes("HỎI") || n.includes("hoi") || n.includes("CÂU")) ||
    wb.SheetNames[0];
  const ws = wb.Sheets[sheetName];
  if (!ws) return 0;
  const rows = XLSX.utils.sheet_to_json(ws, { defval: "" });

  const mapped = rows
    .filter((r) => r["CÂU HỎI"] || r["question"] || r["câu hỏi"])
    .map((r) => ({
      chapter: r["CHƯƠNG"] || r["chapter"] || "Chương mới",
      questionText: r["CÂU HỎI"] || r["question"] || "",
      optionA: r["ĐÁP ÁN A"] || r["option_a"] || "",
      optionB: r["ĐÁP ÁN B"] || r["option_b"] || "",
      optionC: r["ĐÁP ÁN C"] || r["option_c"] || "",
      optionD: r["ĐÁP ÁN D"] || r["option_d"] || "",
      correctAnswer: parseInt(r["ĐÁP ÁN ĐÚNG"] || r["correct_answer"] || 0),
      explanation: r["GIẢI THÍCH"] || r["explanation"] || "",
    }))
    .filter((r) => r.questionText);

  if (!mapped.length) return 0;
  return await batchInsertQuestions(mapped);
}

// ── TEXT BATCH ──
async function importTextBatch(file) {
  const text = await file.text();
  const chapterName = file.name.replace(/\.txt$/i, "").replace(/_/g, " ").trim();
  const parsed = parseQText(text, chapterName);
  if (!parsed.length) return 0;
  return await batchInsertQuestions(parsed);
}

// ── BATCH INSERT helpers — quiz-service chưa có endpoint bulk-insert,
// nên gọi tuần tự từng câu qua createQuestion/createOralQuestion. ──
async function batchInsertQuestions(rows, onProgress) {
  let ok = 0;
  for (let i = 0; i < rows.length; i++) {
    try {
      await createQuestion(rows[i]);
      ok++;
    } catch (err) {
      console.warn("Bỏ qua câu hỏi lỗi:", rows[i].questionText, err.message);
    }
    if (onProgress) onProgress(i + 1, rows.length);
  }
  return ok;
}

async function batchInsertOral(rows) {
  let ok = 0;
  for (const row of rows) {
    try {
      await createOralQuestion(row);
      ok++;
    } catch (err) {
      console.warn("Bỏ qua câu vấn đáp lỗi:", row.questionText, err.message);
    }
  }
  return ok;
}

// ════════════════════════════════════════════
// MATERIALS — Upload & Manage lecture PDFs
// ════════════════════════════════════════════

async function handleMaterialUpload(e) {
  const file = e.target.files[0];
  if (!file) return;

  const title = document.getElementById("matTitle").value.trim();
  const chapter = document.getElementById("matChapter").value.trim();
  const desc = document.getElementById("matDesc").value.trim();
  const status = document.getElementById("matUploadStatus");
  const prog = document.getElementById("matProg");
  const progFill = document.getElementById("matProgFill");

  if (!title) {
    alert("Vui lòng nhập tên tài liệu!");
    return;
  }

  status.style.color = "#1565c0";
  status.textContent = "📤 Đang upload lên máy chủ...";
  prog.style.display = "block";
  progFill.style.width = "20%";

  try {
    const uploaded = await uploadMaterialFile(file);
    const fileUrl = uploaded.fileUrl;
    progFill.style.width = "60%";
    status.textContent = "💾 Đang lưu thông tin...";

    await createMaterial({
      title,
      chapter: chapter || "Chung",
      description: desc,
      fileName: uploaded.fileName,
      fileUrl,
      fileSize: uploaded.fileSize,
      cloudinaryPublicId: uploaded.publicId,
    });

    // ── AI tạo câu hỏi từ PDF qua ai-service ──
    let qCount = 0;
    try {
      progFill.style.width = "70%";
      status.style.color = "#1565c0";
      status.textContent = "🤖 AI đang tạo câu hỏi luyện tập từ nội dung PDF...";

      const pdfResp = await fetch(fileUrl);
      const pdfBuf = await pdfResp.arrayBuffer();
      const pdf = await pdfjsLib.getDocument({ data: pdfBuf }).promise;
      let fullText = "";
      const maxPages = Math.min(pdf.numPages, 40);
      for (let i = 1; i <= maxPages; i++) {
        const page = await pdf.getPage(i);
        const tc = await page.getTextContent();
        fullText += tc.items.map((t) => t.str).join(" ") + "\n";
      }

      if (fullText.trim().length > 100) {
        const chapterName = chapter || title;
        const questions = await aiExtractQuestions(fullText, chapterName);
        if (questions.length > 0) {
          qCount = await batchInsertQuestions(questions);
        }
      }
    } catch (aiErr) {
      console.warn("AI question gen skipped:", aiErr);
    }

    progFill.style.width = "100%";
    status.style.color = "#2e7d32";
    if (qCount > 0) {
      status.textContent = `✅ Upload thành công! AI đã tạo ${qCount} câu hỏi cho "${chapter || title}" trong Luyện tập.`;
      showToast(`Upload + tạo ${qCount} câu hỏi thành công!`, "success");
    } else {
      status.textContent = `✅ Upload thành công: "${title}"! (AI không trích xuất được câu hỏi từ PDF này)`;
      showToast("Upload tài liệu thành công!", "success");
    }

    document.getElementById("matTitle").value = "";
    document.getElementById("matChapter").value = "";
    document.getElementById("matDesc").value = "";
    document.getElementById("matFileInput").value = "";
    loadMaterials();
  } catch (err) {
    progFill.style.width = "100%";
    progFill.style.background = "#ef5350";
    status.style.color = "#c62828";
    status.textContent = "❌ Lỗi: " + err.message;
  }
}

async function loadMaterials() {
  const el = document.getElementById("matList");
  if (!el) return;
  el.innerHTML = '<div style="text-align:center;padding:16px;color:var(--gray-400);">Đang tải...</div>';

  try {
    allMaterials = await listMaterials();
  } catch (err) {
    el.innerHTML = `<div style="text-align:center;padding:24px;color:var(--gray-400);">Lỗi: ${err.message}</div>`;
    return;
  }

  if (!allMaterials.length) {
    el.innerHTML =
      '<div style="text-align:center;padding:24px;color:var(--gray-400);"><i class="fas fa-folder-open" style="font-size:2rem;margin-bottom:8px;display:block;"></i>Chưa có tài liệu nào. Upload tài liệu đầu tiên phía trên!</div>';
    return;
  }

  el.innerHTML = allMaterials
    .map((m) => {
      const size =
        m.fileSize > 1024 * 1024
          ? (m.fileSize / 1024 / 1024).toFixed(1) + " MB"
          : (m.fileSize / 1024).toFixed(0) + " KB";
      const date = new Date(m.createdAtUtc).toLocaleDateString("vi-VN");
      return `<div style="display:flex;align-items:center;gap:12px;padding:12px 0;border-bottom:1px solid var(--gray-100);">
      <div style="width:42px;height:42px;background:#fce4ec;border-radius:10px;display:flex;align-items:center;justify-content:center;font-size:1.2rem;flex-shrink:0;">📄</div>
      <div style="flex:1;min-width:0;">
        <div style="font-weight:700;font-size:0.82rem;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">${m.title}</div>
        <div style="font-size:0.68rem;color:var(--gray-500);">${m.chapter} · ${size} · ${date} · 👁 ${m.viewCount} lượt xem</div>
      </div>
      <div style="display:flex;gap:6px;flex-shrink:0;">
        <a href="${m.fileUrl}" target="_blank" class="btn btn-outline btn-sm" title="Xem file" aria-label="Xem file ${m.title}">
          <i class="fas fa-eye"></i>
        </a>
        <button class="btn btn-sm" style="background:${m.isActive ? "#e8f5e9" : "#fff3e0"};color:${m.isActive ? "#2e7d32" : "#e65100"};border:none;"
          onclick="toggleMaterial('${m.id}')" title="${m.isActive ? "Ẩn" : "Hiện"} tài liệu" aria-label="${m.isActive ? "Ẩn" : "Hiện"} tài liệu ${m.title}">
          <i class="fas fa-${m.isActive ? "eye-slash" : "eye"}"></i>
        </button>
        <button class="btn btn-sm" style="background:#fce4ec;color:#c62828;border:none;"
          onclick="deleteMaterialRow('${m.id}')" title="Xóa" aria-label="Xóa tài liệu ${m.title}">
          <i class="fas fa-trash"></i>
        </button>
      </div>
    </div>`;
    })
    .join("");
}

async function toggleMaterial(id) {
  const m = allMaterials.find((x) => x.id === id);
  if (!m) return;
  try {
    await updateMaterial(id, {
      title: m.title,
      chapter: m.chapter,
      description: m.description,
      isActive: !m.isActive,
    });
  } catch (err) {
    showToast("Lỗi: " + err.message, "error");
    return;
  }
  loadMaterials();
}

async function deleteMaterialRow(id) {
  if (!confirm("Xóa tài liệu này? Học viên sẽ không thể xem nữa.")) return;
  try {
    // content-service deletes the Cloudinary file server-side as part of this call.
    await deleteMaterial(id);
  } catch (err) {
    showToast("Lỗi: " + err.message, "error");
    return;
  }
  showToast("Đã xóa tài liệu", "success");
  loadMaterials();
}

// Drag & drop — chạy ngay khi file này load, nên phải include ở CUỐI <body>
// (sau div#batchDrop trong HTML), giống đúng vị trí script gốc trong admin.html cũ.
const batchDrop = document.getElementById("batchDrop");
if (batchDrop) {
  batchDrop.addEventListener("dragover", (e) => {
    e.preventDefault();
    batchDrop.classList.add("drag-over");
  });
  batchDrop.addEventListener("dragleave", () => batchDrop.classList.remove("drag-over"));
  batchDrop.addEventListener("drop", (e) => {
    e.preventDefault();
    batchDrop.classList.remove("drag-over");
    addBatchFiles(e.dataTransfer.files);
  });
}
