import { render, screen } from "@testing-library/react"
import MatchEndView from "../views/MatchEndView.jsx"
import { vi } from "vitest"

test("MatchEndView sorts players by points", () => {
    const players = [
        { name: "C", points: 1 },
        { name: "A", points: 10 },
        { name: "B", points: 5 },
    ]

    render(<MatchEndView durationInSeconds={10} playerResults={players} />)

    const first = screen.getByText("A")
    const second = screen.getByText("B")
    const third = screen.getByText("C")

    expect(first).toBeInTheDocument()
    expect(second).toBeInTheDocument()
    expect(third).toBeInTheDocument()
})
