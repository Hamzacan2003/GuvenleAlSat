using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace GuvenleAlSat.DataAccess.Entities.Common;

public class BaseEntity
{
    public Guid Id { get; set; }= Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

}
