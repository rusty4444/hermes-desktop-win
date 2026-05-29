using System;
using System.Collections.Generic;

namespace HermesDesktop.WinUI.Models
{
    public class SessionInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public long Timestamp { get; set; }
        public int MessageCount { get; set; }
    }

    public class SessionTranscript
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public long? Timestamp { get; set; }
        public string Error { get; set; }
        public List<SessionMessage> Messages { get; set; } = new List<SessionMessage>();
    }

    public class SessionMessage
    {
        public string Content { get; set; }
        public string Role { get; set; }
        public long? Timestamp { get; set; }
    }

    public class KanbanBoard
    {
        public List<KanbanLane> Lanes { get; set; } = new List<KanbanLane>();
        public List<KanbanCard> Cards { get; set; } = new List<KanbanCard>();
    }

    public class KanbanLane
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public List<KanbanCard> Cards { get; set; } = new List<KanbanCard>();
    }

    public class KanbanCard
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string LaneId { get; set; }
    }

    public class FileItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public long Modified { get; set; }
    }

    public class UsageStats
    {
        public int TotalSessions { get; set; }
        public int TotalMessages { get; set; }
        public int TotalTokens { get; set; }
        public List<string> TopModels { get; set; } = new List<string>();
        public List<RecentSession> RecentSessions { get; set; } = new List<RecentSession>();
    }

    public class RecentSession
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public long Timestamp { get; set; }
        public int MessageCount { get; set; }
        public int TokenCount { get; set; }
    }

    public class SkillInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
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

    // Service-internal result types (used by services for JSON deserialization)
    public class SkillContentResult
    {
        public string Content { get; set; }
        public string Error { get; set; }
    }

    public class SaveResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class ChatTurnResult
    {
        public bool Ok { get; set; }
        public string SessionId { get; set; }
        public string Output { get; set; }
        public string Stdout { get; set; }
        public string Stderr { get; set; }
        public string Error { get; set; }
        public bool TimedOut { get; set; }
    }

    public class FileContentResult
    {
        public string Content { get; set; }
        public string Error { get; set; }
    }

    public class SaveFileResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class FileExistsResult
    {
        public bool Exists { get; set; }
    }

    public class CronJobInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Prompt { get; set; }
        public string Script { get; set; }
        public string Workdir { get; set; }
        public bool NoAgent { get; set; }
        public string ScheduleExpr { get; set; }
        public string State { get; set; }
        public bool Enabled { get; set; }
        public string CreatedAt { get; set; }
        public string NextRunAt { get; set; }
        public string LastRunAt { get; set; }
        public string LastStatus { get; set; }
        public string LastError { get; set; }
        public string Deliver { get; set; }
        public string Model { get; set; }
        public string Provider { get; set; }

        public string StateDisplay => State switch
        {
            "paused" => "Paused",
            "running" => "Running",
            "scheduled" => "Scheduled",
            _ => State ?? "Unknown"
        };
    }
}
