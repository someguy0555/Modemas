import { render, screen, vi } from "@testing-library/react"
import GlobalErrorToast from "../components/GlobalErrorToast"

describe("GlobalErrorToast", () => {
    test("does not render when message is null", () => {
        render(<GlobalErrorToast message={null} />)
        expect(screen.queryByText(/.+/)).toBeNull()
    })

    test("renders error message", () => {
        render(<GlobalErrorToast message="Something went wrong" />)
        expect(screen.getByText("Something went wrong")).toBeInTheDocument()
    })

    // test("calls onClose after duration", () => {
    //     vi.useFakeTimers()
    //     const onClose = vi.fn()
    //
    //     render(
    //         <GlobalErrorToast
    //             message="Error"
    //             duration={2000}
    //             onClose={onClose}
    //         />
    //     )
    //
    //     vi.advanceTimersByTime(2000)
    //     expect(onClose).toHaveBeenCalled()
    //
    //     vi.useRealTimers()
    // })
})
