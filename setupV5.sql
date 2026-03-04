-- ================================================
-- SETUP V5 — Kho tài liệu chung (Materials)
-- Chạy trong SQL Editor SAU setup v1→v4
-- ================================================

-- ================================================
-- BƯỚC 1: Bảng materials — lưu metadata tài liệu
-- ================================================
CREATE TABLE IF NOT EXISTS public.materials (
  id          UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  title       TEXT NOT NULL,                    -- Tên hiển thị VD: "Chương 1 - Khái niệm TTHCM"
  chapter     TEXT DEFAULT '',                  -- Chương tương ứng
  description TEXT DEFAULT '',                  -- Mô tả ngắn
  file_name   TEXT NOT NULL,                    -- Tên file gốc
  file_url    TEXT NOT NULL,                    -- URL trên Supabase Storage
  file_size   INTEGER DEFAULT 0,                -- Kích thước bytes
  uploaded_by UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  is_active   BOOLEAN DEFAULT true,             -- Ẩn/hiện tài liệu
  view_count  INTEGER DEFAULT 0,
  created_at  TIMESTAMPTZ DEFAULT NOW()
);

ALTER TABLE public.materials ENABLE ROW LEVEL SECURITY;

-- Học viên đọc tất cả tài liệu đang active
CREATE POLICY "mat_select" ON public.materials
  FOR SELECT USING (is_active = true OR public.get_my_role() IN ('teacher','admin'));

-- Chỉ teacher/admin thêm tài liệu
CREATE POLICY "mat_insert" ON public.materials
  FOR INSERT WITH CHECK (public.get_my_role() IN ('teacher','admin'));

-- Chỉ teacher/admin sửa/xóa
CREATE POLICY "mat_update" ON public.materials
  FOR UPDATE USING (public.get_my_role() IN ('teacher','admin'));

CREATE POLICY "mat_delete" ON public.materials
  FOR DELETE USING (public.get_my_role() IN ('teacher','admin'));

-- ================================================
-- BƯỚC 2: Tăng view count khi học viên mở tài liệu
-- ================================================
CREATE OR REPLACE FUNCTION public.increment_material_views(material_id UUID)
RETURNS VOID AS $$
  UPDATE public.materials SET view_count = view_count + 1 WHERE id = material_id;
$$ LANGUAGE SQL SECURITY DEFINER;

-- ================================================
-- BƯỚC 3: Kiểm tra
-- ================================================
SELECT 'materials table' AS check_item, COUNT(*)::TEXT AS result
FROM information_schema.tables
WHERE table_schema='public' AND table_name='materials';