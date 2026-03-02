import { render, screen, fireEvent } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import MainMenuView from './MainMenuView'

describe('MainMenuView', () => {
    it('renders main menu UI', () => {
        render(<MainMenuView />)

        expect(screen.getByText('Main Menu')).toBeInTheDocument()
        expect(screen.getByPlaceholderText('Enter your name')).toBeInTheDocument()
        expect(screen.getByPlaceholderText('Enter Lobby ID')).toBeInTheDocument()
        expect(screen.getByText('Create Lobby')).toBeInTheDocument()
        expect(screen.getByText('Join Lobby')).toBeInTheDocument()
    })

    it('allows typing into name and lobby inputs', () => {
        render(<MainMenuView />)

        const nameInput = screen.getByPlaceholderText('Enter your name')
        const lobbyInput = screen.getByPlaceholderText('Enter Lobby ID')

        fireEvent.change(nameInput, { target: { value: 'Alice' } })
        fireEvent.change(lobbyInput, { target: { value: '1234' } })

        expect(nameInput.value).toBe('Alice')
        expect(lobbyInput.value).toBe('1234')
    })

    it('buttons are clickable without crashing', () => {
        render(<MainMenuView />)

        fireEvent.click(screen.getByText('Create Lobby'))
        fireEvent.click(screen.getByText('Join Lobby'))

        // If no error is thrown, the test passes
        expect(true).toBe(true)
    })
})
