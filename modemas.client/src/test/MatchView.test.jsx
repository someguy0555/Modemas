import { render, screen, fireEvent } from "@testing-library/react"
import MatchView from "../views/MatchView.jsx"
import { vi } from "vitest"

const baseQuestion = {
    type: "MultipleChoice",
    text: "What is 2 + 2?",
    choices: ["3", "4", "5"],
    timeLimit: 10
}

describe("MatchView", () => {
    test("shows waiting text when question is null", () => {
        render(<MatchView question={null} />)
        expect(screen.getByText(/Waiting for question/i)).toBeInTheDocument()
    })

    test("renders question text and choices", () => {
        render(
            <MatchView
                question={baseQuestion}
                setIsCorrect={vi.fn()}
            />
        )

        expect(screen.getByText("What is 2 + 2?")).toBeInTheDocument()
        expect(screen.getByText("3")).toBeInTheDocument()
        expect(screen.getByText("4")).toBeInTheDocument()
        expect(screen.getByText("5")).toBeInTheDocument()
    })

    test("invokes AnswerQuestion when clicking choice", () => {
        const invoke = vi.fn()
        const connection = { invoke }

        render(
            <MatchView
                connection={connection}
                lobbyId="123"
                question={baseQuestion}
                answered={false}
                setAnswered={vi.fn()}
                setIsCorrect={vi.fn()}
            />
        )

        fireEvent.click(screen.getByText("4"))
        expect(invoke).toHaveBeenCalledWith("AnswerQuestion", "123", 1)
    })

    test("renders true/false buttons", () => {
        render(
            <MatchView
                question={{
                    type: "TrueFalse",
                    text: "Is sky blue?",
                    timeLimit: 5,
                }}
                answered={false}
                setAnswered={vi.fn()}
                setIsCorrect={vi.fn()}
            />
        )

        expect(screen.getByText("True")).toBeInTheDocument()
        expect(screen.getByText("False")).toBeInTheDocument()
    })

    test("submit button disabled when no answers selected", () => {
        render(
            <MatchView
                question={{
                    type: "MultipleAnswer",
                    text: "Pick evens",
                    choices: ["1", "2"],
                    timeLimit: 5,
                }}
                answered={false}
                setAnswered={vi.fn()}
                setIsCorrect={vi.fn()}
            />
        )

        expect(screen.getByText("Submit Answers")).toBeDisabled()
    })
})
