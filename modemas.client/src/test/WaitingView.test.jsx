import { render, screen, fireEvent } from "@testing-library/react"
import WaitingView from "../views/WaitingView.jsx"
import { vi } from "vitest"

describe("WaitingView", () => {
    test("renders lobby info", () => {
        render(
            <WaitingView
                lobbyId="ABC123"
                lobbyState="Waiting"
                playerName="Alice"
                players={["Alice", "Bob"]}
                isHost={false}
            />
        )

        expect(screen.getByText("ABC123")).toBeInTheDocument()
        expect(screen.getAllByText("Alice").length).toBeGreaterThan(0)
        expect(screen.getByText("Bob")).toBeInTheDocument()
    })

    test("host sees lobby customization controls", () => {
        render(
            <WaitingView
                lobbyId="1"
                lobbyState="Waiting"
                playerName="Host"
                players={["Host"]}
                isHost={true}
            />
        )

        expect(screen.getByText(/Lobby Customization/i)).toBeInTheDocument()
        expect(screen.getByText(/Save Settings/i)).toBeInTheDocument()
        expect(screen.getByText(/Start Match/i)).toBeInTheDocument()
    })

    test("start match invokes StartVoting", () => {
        const invoke = vi.fn()
        const connection = {
            invoke,
            on: vi.fn(),
            off: vi.fn(),
        }

        render(
            <WaitingView
                connection={connection}
                lobbyId="LOBBY1"
                lobbyState="Waiting"
                playerName="Host"
                players={["Host"]}
                isHost={true}
            />
        )

        fireEvent.click(screen.getByText("Start Match"))
        expect(invoke).toHaveBeenCalledWith("StartVoting", "LOBBY1")
    })

    test("non-host does not see host controls", () => {
        render(
            <WaitingView
                lobbyId="X"
                lobbyState="Waiting"
                playerName="Alice"
                players={["Alice"]}
                isHost={false}
            />
        )

        expect(screen.queryByText("Start Match")).not.toBeInTheDocument()
    })

    // test("host can save lobby settings", () => {
    //     const invoke = vi.fn()
    //     const connection = { invoke, on: vi.fn(), off: vi.fn() }
    //
    //     render(
    //         <WaitingView
    //             connection={connection}
    //             lobbyId="L1"
    //             lobbyState="Waiting"
    //             playerName="Host"
    //             players={["Host"]}
    //             isHost={true}
    //         />
    //     )
    //
    //     fireEvent.click(screen.getByText("Save Settings"))
    //     expect(invoke).toHaveBeenCalled()
    // })

    // test("leave lobby reloads page", async () => {
    //     const connection = {
    //         stop: vi.fn(),
    //         on: vi.fn(),
    //         off: vi.fn(),
    //     }
    //
    //     vi.spyOn(window, "confirm").mockReturnValue(true)
    //     vi.spyOn(window.location, "reload").mockImplementation(() => { })
    //
    //     render(
    //         <WaitingView
    //             connection={connection}
    //             lobbyId="L"
    //             lobbyState="Waiting"
    //             playerName="A"
    //             players={["A"]}
    //             isHost={false}
    //         />
    //     )
    //
    //     fireEvent.click(screen.getByText("← Leave"))
    //     expect(connection.stop).toHaveBeenCalled()
    // })
})
