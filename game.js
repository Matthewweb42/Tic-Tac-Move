// Game state
let board = ['', '', '', '', '', '', '', '', ''];
let currentPlayer = 'X';
let gameActive = true;
let gameMode = 'place'; // 'place' or 'move'
let selectedPiece = null;
let piecesPlaced = { X: 0, O: 0 };
const maxPieces = 3;

// Win conditions
const winConditions = [
    [0, 1, 2],
    [3, 4, 5],
    [6, 7, 8],
    [0, 3, 6],
    [1, 4, 7],
    [2, 5, 8],
    [0, 4, 8],
    [2, 4, 6]
];

// DOM elements
const cells = document.querySelectorAll('.cell');
const statusDisplay = document.getElementById('status');
const modeStatusDisplay = document.getElementById('mode-status');
const resetBtn = document.getElementById('reset-btn');
const toggleModeBtn = document.getElementById('toggle-mode-btn');

// Initialize game
function init() {
    cells.forEach((cell, index) => {
        cell.addEventListener('click', () => handleCellClick(index));
    });
    resetBtn.addEventListener('click', resetGame);
    toggleModeBtn.addEventListener('click', toggleMode);
    updateDisplay();
}

// Handle cell click
function handleCellClick(index) {
    if (!gameActive) return;

    if (gameMode === 'place') {
        handlePlaceMode(index);
    } else {
        handleMoveMode(index);
    }
}

// Handle placing pieces
function handlePlaceMode(index) {
    // Check if cell is already occupied
    if (board[index] !== '') return;

    // Check if player has already placed max pieces
    if (piecesPlaced[currentPlayer] >= maxPieces) return;

    // Place the piece
    board[index] = currentPlayer;
    piecesPlaced[currentPlayer]++;
    updateBoard();

    // Check for winner
    if (checkWinner()) {
        gameActive = false;
        statusDisplay.textContent = `Player ${currentPlayer} Wins!`;
        return;
    }

    // Check if all pieces are placed
    if (piecesPlaced.X === maxPieces && piecesPlaced.O === maxPieces) {
        gameMode = 'move';
        toggleModeBtn.disabled = false;
        modeStatusDisplay.textContent = 'Mode: Move Pieces';
    }

    // Switch player
    currentPlayer = currentPlayer === 'X' ? 'O' : 'X';
    updateDisplay();
}

// Handle moving pieces
function handleMoveMode(index) {
    const cell = cells[index];

    // If no piece is selected
    if (selectedPiece === null) {
        // Check if the clicked cell belongs to current player
        if (board[index] === currentPlayer) {
            selectedPiece = index;
            cell.classList.add('selected');
            highlightAdjacentCells(index);
        }
        return;
    }

    // If a piece is already selected
    if (selectedPiece !== null) {
        // If clicking the same piece, deselect it
        if (selectedPiece === index) {
            deselectPiece();
            return;
        }

        // Check if the target cell is empty and adjacent
        if (board[index] === '' && isAdjacent(selectedPiece, index)) {
            // Move the piece
            board[index] = board[selectedPiece];
            board[selectedPiece] = '';
            deselectPiece();
            updateBoard();

            // Check for winner
            if (checkWinner()) {
                gameActive = false;
                statusDisplay.textContent = `Player ${currentPlayer} Wins!`;
                return;
            }

            // Switch player
            currentPlayer = currentPlayer === 'X' ? 'O' : 'X';
            updateDisplay();
        } else {
            // Invalid move, deselect and potentially select new piece
            deselectPiece();
            if (board[index] === currentPlayer) {
                selectedPiece = index;
                cells[index].classList.add('selected');
                highlightAdjacentCells(index);
            }
        }
    }
}

// Check if two cells are adjacent
function isAdjacent(from, to) {
    const row1 = Math.floor(from / 3);
    const col1 = from % 3;
    const row2 = Math.floor(to / 3);
    const col2 = to % 3;

    const rowDiff = Math.abs(row1 - row2);
    const colDiff = Math.abs(col1 - col2);

    // Adjacent means one step horizontally, vertically, or diagonally
    return rowDiff <= 1 && colDiff <= 1 && (rowDiff + colDiff > 0);
}

// Highlight adjacent empty cells
function highlightAdjacentCells(index) {
    for (let i = 0; i < 9; i++) {
        if (board[i] === '' && isAdjacent(index, i)) {
            cells[i].classList.add('highlight');
        }
    }
}

// Deselect piece
function deselectPiece() {
    if (selectedPiece !== null) {
        cells[selectedPiece].classList.remove('selected');
        selectedPiece = null;
    }
    // Remove all highlights
    cells.forEach(cell => cell.classList.remove('highlight'));
}

// Toggle between place and move mode (manual override)
function toggleMode() {
    if (piecesPlaced.X < maxPieces || piecesPlaced.O < maxPieces) {
        return; // Can't switch if not all pieces are placed
    }

    gameMode = gameMode === 'place' ? 'move' : 'place';
    deselectPiece();
    updateDisplay();
}

// Check for winner
function checkWinner() {
    for (let condition of winConditions) {
        const [a, b, c] = condition;
        if (board[a] && board[a] === board[b] && board[a] === board[c]) {
            return true;
        }
    }
    return false;
}

// Update board display
function updateBoard() {
    cells.forEach((cell, index) => {
        cell.textContent = board[index];
        cell.className = 'cell';
        if (board[index] === 'X') {
            cell.classList.add('x');
        } else if (board[index] === 'O') {
            cell.classList.add('o');
        }
    });
}

// Update status display
function updateDisplay() {
    if (gameActive) {
        statusDisplay.textContent = `Player ${currentPlayer}'s Turn`;
        if (gameMode === 'place') {
            const remaining = maxPieces - piecesPlaced[currentPlayer];
            modeStatusDisplay.textContent = `Mode: Place Piece (${remaining} remaining)`;
        } else {
            modeStatusDisplay.textContent = 'Mode: Move Pieces';
        }
    }
}

// Reset game
function resetGame() {
    board = ['', '', '', '', '', '', '', '', ''];
    currentPlayer = 'X';
    gameActive = true;
    gameMode = 'place';
    selectedPiece = null;
    piecesPlaced = { X: 0, O: 0 };
    toggleModeBtn.disabled = true;
    deselectPiece();
    updateBoard();
    updateDisplay();
}

// Initialize the game when the page loads
init();
