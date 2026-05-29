using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace HermesDesktop.WinUI.Services
{
    /// <summary>
    /// Service for managing workflow presets (stored locally).
    /// </summary>
    public class WorkflowService
    {
        private readonly string _workflowsFilePath;

        public WorkflowService()
        {
            // Store workflows in the local application data folder.
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDataPath = Path.Combine(localAppData, "HermesDesktop.WinUI");
            Directory.CreateDirectory(appDataPath);
            _workflowsFilePath = Path.Combine(appDataPath, "workflows.json");
        }

        /// <summary>
        /// Gets all workflows.
        /// </summary>
        public async Task<IEnumerable<Workflow>> GetWorkflowsAsync()
        {
            if (!File.Exists(_workflowsFilePath))
            {
                return await Task.FromResult(Array.Empty<Workflow>());
            }

            var json = await File.ReadAllTextAsync(_workflowsFilePath);
            return JsonSerializer.Deserialize<List<Workflow>>(json) ?? new List<Workflow>();
        }

        /// <summary>
        /// Saves a workflow.
        /// </summary>
        public async Task SaveWorkflowAsync(Workflow workflow)
        {
            var workflows = (await GetWorkflowsAsync()).ToList();
            var existing = workflows.FirstOrDefault(w => w.Id == workflow.Id);
            if (existing != null)
            {
                workflows.Remove(existing);
            }
            workflows.Add(workflow);

            var json = JsonSerializer.Serialize(workflows, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_workflowsFilePath, json);
        }

        /// <summary>
        /// Deletes a workflow by ID.
        /// </summary>
        public async Task DeleteWorkflowAsync(string id)
        {
            var workflows = (await GetWorkflowsAsync()).ToList();
            var workflow = workflows.FirstOrDefault(w => w.Id == id);
            if (workflow != null)
            {
                workflows.Remove(workflow);
                var json = JsonSerializer.Serialize(workflows, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_workflowsFilePath, json);
            }
        }
    }

    public class Workflow
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string HermesProfile { get; set; } = string.Empty;
        public string InitialPrompt { get; set; } = string.Empty;
        public List<string> SkillIds { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
