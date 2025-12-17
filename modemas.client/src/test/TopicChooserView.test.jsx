import { render, screen } from "@testing-library/react"
import TopicChooserView from "../views/TopicChooserView.jsx"

test("TopicChooserView shows loading text", () => {
    render(<TopicChooserView />)
    expect(screen.getByText(/Loading/i)).toBeInTheDocument()
})
