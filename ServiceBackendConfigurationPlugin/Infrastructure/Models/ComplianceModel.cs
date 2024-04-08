using System;
using System.Collections.Generic;

namespace ServiceBackendConfigurationPlugin.Infrastructure.Models;

public class ComplianceModel
{
    public int Id { get; set; }

    public string ControlArea { get; set; }

    public string ItemName { get; set; }

    public DateTime? Deadline { get; set; }

    public List<KeyValuePair<int, string>> Responsible { get; set; }

    public int? ComplianceTypeId { get; set; }

    public int PlanningId { get; set; }

    public int EformId { get; set; }

    public int CaseId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string FolderName { get; set; }

    public string WorkflowState { get; set; }
}