import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import MainMenuView from './MainMenuView'

// Mock fetch for server stats
beforeEach(() => {
    global.fetch = vi.fn(() =>
        Promise.resolve({
            ok: true,
            json: () =>
                Promise.resolve({
                    totalLobbies: 1,
                    activeLobbies: 1,
                    totalPlayers: 2,
                    averagePlayersPerLobby: 2,
                    activeTopics: ['Test']
                })
        })
    )
})

describe('MainMenuView', () => {
    const mockSetGlobalPlayerName = vi.fn()
    const mockSetGlobalLobbyId = vi.fn()
    const mockConnection = {
        invoke: vi.fn()
    }

    it('renders main menu UI', async () => {
        render(
            <MainMenuView
                setGlobalPlayerName={mockSetGlobalPlayerName}
                setGlobalLobbyId={mockSetGlobalLobbyId}
                connection={mockConnection}
            />
        )

        expect(screen.getByText('Main Menu')).toBeInTheDocument()
        expect(screen.getByPlaceholderText('Enter your name')).toBeInTheDocument()
        expect(screen.getByPlaceholderText('Enter Lobby ID')).toBeInTheDocument()
        expect(screen.getByText('Create Lobby')).toBeInTheDocument()
        expect(screen.getByText('Join Lobby')).toBeInTheDocument()

        // server stats eventually render
        await waitFor(() =>
            expect(screen.getByText('Server Stats')).toBeInTheDocument()
        )
    })

    it('creates lobby when Create Lobby is clicked', () => {
        render(
            <MainMenuView
                setGlobalPlayerName={mockSetGlobalPlayerName}
                setGlobalLobbyId={mockSetGlobalLobbyId}
                connection={mockConnection}
            />
        )

        fireEvent.change(screen.getByPlaceholderText('Enter your name'), {
            target: { value: 'Alice' }
        })

        fireEvent.click(screen.getByText('Create Lobby'))

        expect(mockSetGlobalPlayerName).toHaveBeenCalledWith('Alice')
        expect(mockConnection.invoke).toHaveBeenCalledWith('CreateLobby', 'Alice')
    })

    it('joins lobby when Join Lobby is clicked', () => {
        render(
            <MainMenuView
                setGlobalPlayerName={mockSetGlobalPlayerName}
                setGlobalLobbyId={mockSetGlobalLobbyId}
                connection={mockConnection}
            />
        )

        fireEvent.change(screen.getByPlaceholderText('Enter your name'), {
            target: { value: 'Bob' }
        })

        fireEvent.change(screen.getByPlaceholderText('Enter Lobby ID'), {
            target: { value: '1234' }
        })

        fireEvent.click(screen.getByText('Join Lobby'))

        expect(mockSetGlobalPlayerName).toHaveBeenCalledWith('Bob')
        expect(mockSetGlobalLobbyId).toHaveBeenCalledWith('1234')
        expect(mockConnection.invoke).toHaveBeenCalledWith(
            'JoinLobby',
            '1234',
            'Bob'
        )
    })
})
