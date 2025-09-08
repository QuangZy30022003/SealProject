using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Repositories.Models;

public partial class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
