-- Tạo bảng test FT600_dev từ FT600 (cấu trúc + dữ liệu).
-- Bật trong appsettings.json:  "Dev": { "UseDevTables": true }
-- Chạy trên DOGE_WH. Chỉ chạy 1 lần; muốn làm mới dữ liệu thì DROP TABLE rồi chạy lại.
IF OBJECT_ID('dbo.FT600_dev', 'U') IS NOT NULL
BEGIN
    RAISERROR('FT600_dev đã tồn tại. DROP TABLE dbo.FT600_dev nếu muốn tạo lại.', 16, 1);
    RETURN;
END

SELECT * INTO dbo.FT600_dev FROM dbo.FT600;

-- SELECT INTO không copy PK/default/index → tạo lại PK trên cột id (Guid).
-- id phải NOT NULL mới tạo được PK.
ALTER TABLE dbo.FT600_dev ALTER COLUMN id uniqueidentifier NOT NULL;
ALTER TABLE dbo.FT600_dev ADD CONSTRAINT PK_FT600_dev PRIMARY KEY (id);

-- Nếu FT600 có default constraint (vd. Actived = 1) / index khác, script lại bằng
-- SSMS > Generate Scripts rồi đổi tên đối tượng sang *_dev.
-- Kiểm tra nhanh:
SELECT (SELECT COUNT(*) FROM dbo.FT600) AS FT600_rows, (SELECT COUNT(*) FROM dbo.FT600_dev) AS FT600_dev_rows;
