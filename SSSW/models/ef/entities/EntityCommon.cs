using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SSSW.Model;

public class EntityCommon
{
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy HH:mm:ss}")]
    public DateTime? CreatedDate { get; set; } = null;

    [Browsable(false)]
    public int? Mesoyear { get; set; } = 1500;

    [Browsable(false)]
    public string? Mesocomp { get; set; } = null;

    [Browsable(false)]
    public bool? Actived { get; set; } = true;

    [Browsable(false)]
    public string? CreatedBy { get; set; } = null;

    [Browsable(false)]
    public string? CreatedMachine { get; set; } = null;

    [Browsable(false)]
    public string? ModifiedBy { get; set; } = null;

    [Browsable(false)]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy HH:mm:ss}")]
    public DateTime? ModifiedDate { get; set; } = null;

    [Browsable(false)]
    public string? ModifiedMachine { get; set; } = null;

    [Browsable(false)]
    public Guid? TransactionId { get; set; } = Guid.Empty;
}
