import { render, screen, fireEvent } from "@testing-library/react"
import MainMenuView from "../views/MainMenuView.jsx"

describe("MainMenuView", () => {
    test("user can type in name & lobby ID", () => {
        render(<MainMenuView />)

        const nameInput = screen.getByPlaceholderText("Enter your name")
        const lobbyInput = screen.getByPlaceholderText("Enter Lobby ID")

        fireEvent.change(nameInput, { target: { value: "Alice" } })
        fireEvent.change(lobbyInput, { target: { value: "1234" } })

        expect(nameInput.value).toBe("Alice")
        expect(lobbyInput.value).toBe("1234")
    })

    test("clicking Create Lobby invokes connection method", async () => {
        const fakeInvoke = vi.fn()
        const connection = { invoke: fakeInvoke }

        const setName = vi.fn()
        render(<MainMenuView connection={connection} setGlobalPlayerName={setName} />)

        fireEvent.change(screen.getByPlaceholderText("Enter your name"), {
            target: { value: "Bob" },
        })

        fireEvent.click(screen.getByText("Create Lobby"))

        expect(setName).toHaveBeenCalledWith("Bob")
        expect(fakeInvoke).toHaveBeenCalledWith("CreateLobby", "Bob")
    })
})
