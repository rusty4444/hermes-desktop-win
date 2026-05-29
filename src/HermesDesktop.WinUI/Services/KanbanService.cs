using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
import json
import os

def get_kanban_board():
    # We assume the Kanban data is stored in ~/.hermes/kanban/board.json
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
            var result = await _sshTransport.ExecuteJSONAsync<KanbanBoard>(pythonScript);
            return result;
        }

        /// <summary>
        /// Creates a new card in the specified lane.
        /// </summary>
        public async Task CreateCardAsync(string laneId, string title, string description = null)
        {
            var pythonScript = $@"
import json
import os

lane_id = {json.dumps(laneId)}
title = {json.dumps(title)}
description = {json.dumps(description)}

def create_card(lane_id, title, description):
    kanban_dir = os.path.expanduser('~/.hermes/kanban')
    board_path = os.path.join(kanban_dir, 'board.json')
    if not os.path.isfile(board_path):
        # Initialize the board if it doesn't exist
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
        # If the lane doesn't exist, we create it (or we could return an error)
        # For simplicity, we'll just add the lane.
        data.setdefault('lanes', []).append({'id': lane_id, 'title': lane_id, 'cards': []})

    # Create the card
    card = {
        'id': 'card_' + str(int(os.time() * 1000)),  # Simple ID generation
        'title': title,
        'description': description or '',
        'lane_id': lane_id
    }
    data.setdefault('cards', []).append(card)

    # Save the board
    os.makedirs(kanban_dir, exist_ok=True)
    with open(board_path, 'w') as f:
        json.dump(data, f, indent=2)

if __name__ == '__main__':
    create_card(lane_id, title, description)
";
            await _sshTransport.ExecuteAsync(pythonScript);
        }

        /// <summary>
        /// Updates a card (e.g., move to another lane, update title/description).
        /// </summary>
        public async Task UpdateCardAsync(string cardId, string laneId = null, string title = null, string description = null)
        {
            var pythonScript = $@"
import json
import os

card_id = {json.dumps(cardId)}
lane_id = {json.dumps(laneId)} if {json.dumps(laneId != null)} else None
title = {json.dumps(title)} if {json.dumps(title != null)} else None
description = {json.dumps(description)} if {json.dumps(description != null)} else None

def update_card(card_id, lane_id, title, description):
    kanban_dir = os.path.expanduser('~/.hermes/kanban')
    board_path = os.path.join(kanban_dir, 'board.json')
    if not os.path.isfile(board_path):
        return  # Nothing to update

    try:
        with open(board_path, 'r') as f:
            data = json.load(f)
    except Exception:
        return

    # Find the card
    card = None
    for c in data.get('cards', []):
        if c.get('id') == card_id:
            card = c
            break
    if card is None:
        return  # Card not found

    # Update the card
    if lane_id is not None:
        card['lane_id'] = lane_id
    if title is not None:
        card['title'] = title
    if description is not None:
        card['description'] = description

    # Save the board
    with open(board_path, 'w') as f:
        json.dump(data, f, indent=2)

if __name__ == '__main__':
    update_card(card_id, lane_id, title, description)
";
            await _sshTransport.ExecuteAsync(pythonScript);
        }

        /// <summary>
        /// Deletes a card.
        /// </summary>
        public async Task DeleteCardAsync(string cardId)
        {
            var pythonScript = $@"
import json
import os

card_id = {json.dumps(cardId)}

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

    # Remove the card
    data['cards'] = [c for c in data.get('cards', []) if c.get('id') != card_id]

    # Save the board
    with open(board_path, 'w') as f:
        json.dump(data, f, indent=2)

if __name__ == '__main__':
    delete_card(card_id)
";
            await _sshTransport.ExecuteAsync(pythonScript);
        }
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
        // We don't store the cards in the lane; we have a separate list of cards.
    }

    public class KanbanCard
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string LaneId { get; set; }
    }
}
