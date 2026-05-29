using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;

namespace HermesDesktop.WinUI.Services
{
    /// <summary>
    /// Service for interacting with the Hermes Kanban board on the remote host.
    /// </summary>
    public class KanbanService
    {
        private readonly SSHTransport _sshTransport;

        public KanbanService(SSHTransport sshTransport)
        {
            _sshTransport = sshTransport ?? throw new ArgumentNullException(nameof(sshTransport));
        }

        /// <summary>
        /// Gets the Kanban board state (lanes and cards).
        /// </summary>
        public async Task<KanbanBoard> GetBoardAsync()
        {
            var pythonScript = @"
import json, os, time

def get_kanban_board():
    kanban_dir = os.path.expanduser('~/.hermes/kanban')
    board_path = os.path.join(kanban_dir, 'board.json')
    if not os.path.isfile(board_path):
        return {'lanes': [], 'cards': []}

    try:
        with open(board_path, 'r') as f:
            data = json.load(f)
        return data
    except Exception:
        return {'lanes': [], 'cards': []}

if __name__ == '__main__':
    result = get_kanban_board()
    print(json.dumps(result))
";
            return await _sshTransport.ExecuteJSONAsync<KanbanBoard>(pythonScript);
        }

        /// <summary>
        /// Creates a new card in the specified lane.
        /// </summary>
        public async Task CreateCardAsync(string laneId, string title, string description = null)
        {
            var pyLaneId = JsonSerializer.Serialize(laneId);
            var pyTitle = JsonSerializer.Serialize(title);
            var pyDesc = JsonSerializer.Serialize(description ?? "");
            var pythonScript = @"
import json, os, time

lane_id = " + pyLaneId + @"
title = " + pyTitle + @"
description = " + pyDesc + @"

def create_card(lane_id, title, description):
    kanban_dir = os.path.expanduser('~/.hermes/kanban')
    board_path = os.path.join(kanban_dir, 'board.json')

    if not os.path.isfile(board_path):
        data = {'lanes': [], 'cards': []}
    else:
        try:
            with open(board_path, 'r') as f:
                data = json.load(f)
        except Exception:
            data = {'lanes': [], 'cards': []}

    # Ensure the lane exists
    lane_exists = any(lane.get('id') == lane_id for lane in data.get('lanes', []))
    if not lane_exists:
        data.setdefault('lanes', []).append({'id': lane_id, 'title': lane_id})

    card = {
        'id': 'card_' + str(int(time.time() * 1000)),
        'title': title,
        'description': description,
        'laneId': lane_id
    }
    data.setdefault('cards', []).append(card)

    os.makedirs(kanban_dir, exist_ok=True)
    with open(board_path, 'w') as f:
        json.dump(data, f, indent=2)

if __name__ == '__main__':
    create_card(lane_id, title, description)
";
            var result = await _sshTransport.ExecuteAsync(pythonScript);
            _sshTransport.ValidateSuccessfulExit(result);
        }

        /// <summary>
        /// Updates a card (move to another lane, update title/description).
        /// </summary>
        public async Task UpdateCardAsync(string cardId, string laneId = null, string title = null, string description = null)
        {
            var pyCardId = JsonSerializer.Serialize(cardId);
            var hasLane = laneId != null ? "True" : "False";
            var pyLaneId = JsonSerializer.Serialize(laneId ?? "");
            var hasTitle = title != null ? "True" : "False";
            var pyTitle = JsonSerializer.Serialize(title ?? "");
            var hasDesc = description != null ? "True" : "False";
            var pyDesc = JsonSerializer.Serialize(description ?? "");

            var pythonScript = @"
import json, os

card_id = " + pyCardId + @"
has_lane = " + hasLane + @"
lane_id = " + pyLaneId + @"
has_title = " + hasTitle + @"
title = " + pyTitle + @"
has_desc = " + hasDesc + @"
description = " + pyDesc + @"

def update_card(card_id, lane_id, title, description, has_lane, has_title, has_desc):
    kanban_dir = os.path.expanduser('~/.hermes/kanban')
    board_path = os.path.join(kanban_dir, 'board.json')
    if not os.path.isfile(board_path):
        return

    try:
        with open(board_path, 'r') as f:
            data = json.load(f)
    except Exception:
        return

    for card in data.get('cards', []):
        if card.get('id') == card_id:
            if has_lane:
                card['laneId'] = lane_id
            if has_title:
                card['title'] = title
            if has_desc:
                card['description'] = description
            break

    with open(board_path, 'w') as f:
        json.dump(data, f, indent=2)

if __name__ == '__main__':
    update_card(card_id, lane_id, title, description, has_lane, has_title, has_desc)
";
            var result = await _sshTransport.ExecuteAsync(pythonScript);
            _sshTransport.ValidateSuccessfulExit(result);
        }

        /// <summary>
        /// Deletes a card by ID.
        /// </summary>
        public async Task DeleteCardAsync(string cardId)
        {
            var pyCardId = JsonSerializer.Serialize(cardId);
            var pythonScript = @"
import json, os

card_id = " + pyCardId + @"

def delete_card(card_id):
    kanban_dir = os.path.expanduser('~/.hermes/kanban')
    board_path = os.path.join(kanban_dir, 'board.json')
    if not os.path.isfile(board_path):
        return

    try:
        with open(board_path, 'r') as f:
            data = json.load(f)
    except Exception:
        return

    data['cards'] = [c for c in data.get('cards', []) if c.get('id') != card_id]

    with open(board_path, 'w') as f:
        json.dump(data, f, indent=2)

if __name__ == '__main__':
    delete_card(card_id)
";
            var result = await _sshTransport.ExecuteAsync(pythonScript);
            _sshTransport.ValidateSuccessfulExit(result);
        }
    }
}
