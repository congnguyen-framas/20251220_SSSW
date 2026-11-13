/*
    sp_sssw_getGroupSize
    Trả về trọng lượng ép (ShotWeight = c063 + c064) theo từng size của nhóm item (cùng khuôn / cùng tiền tố mã).

    @ItemCode : phần đầu của mã item, ví dụ '1-VB042502-B660' (store tự thêm '-%' phía sau để khớp mọi size của nhóm).
    @MesoYear : năm Winline (mặc định 1512 như query gốc).

    Ví dụ:  EXEC sp_sssw_getGroupSize @ItemCode = '1-VB042502-B660'
    Model C#: SSSW.models.GroupSizeModel
*/
CREATE OR ALTER PROCEDURE dbo.sp_sssw_getGroupSize
    @ItemCode NVARCHAR(100),
    @MesoYear INT = 1512
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        _v021.c002                    AS ItemCode,
        _v021.c003                    AS ItemName,
        _v021.c067                    AS SizeCode,
        _t314.c002                    AS SizeName,
        _v021.c063                    AS C063,
        _v021.c064                    AS C064,
        (_v021.c063 + _v021.c064)     AS ShotWeight
    FROM [vnt86]..v021 _v021
        INNER JOIN vnt86..t314 _t314
            ON _t314.c003 = _v021.c067
           AND _t314.mesoyear = _v021.mesoyear
    WHERE _v021.mesoyear = @MesoYear
      AND _v021.c002 LIKE @ItemCode + '-%';
END
GO
