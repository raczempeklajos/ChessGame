
import ChessBoard from "./components/ChessBoard";

function App() {
  return (
    <div className="site-wrapper">
      <header className="site-header">
        <a className="logo" href="#">
          CHESSGAME<span>®</span>
        </a>

        <nav className="navigation">
          <a className="active" href="#play">Play</a>
          <a href="#about">About</a>
        </nav>

        <div className="header-label">EST. 2026</div>
      </header>

      <main id="play" className="main-content">
        <section className="hero">
          <div className="hero-content">
            <span className="eyebrow">
              THE CHESS EXPERIENCE — 001
            </span>

            <h1>
              A different
              <br />
              way to <em>play.</em>
            </h1>

            <p className="hero-description">
              Minimal design. Classic strategy.
              A new perspective on the timeless game.
            </p>

            <a className="hero-link" href="#board">
              EXPLORE THE GAME
              <span aria-hidden="true">↗</span>
            </a>
          </div>

          <div id="board" className="board-section">
            <div className="board-header">
              <span>01 / GAME BOARD</span>
              <span>WHITE TO MOVE</span>
            </div>

            <ChessBoard />

            <div className="board-footer">
              <span>PLAYER VS COMPUTER</span>
              <span>CHESS / 2026</span>
            </div>
          </div>
        </section>

        <section id="about" className="about-section">
          <span>ABOUT THE PROJECT</span>
          <p>
            A modern chess experience built with
            React, TypeScript and C#.
          </p>
        </section>
      </main>

      <footer className="site-footer">
        <span>CHESSGAME®</span>
        <span>DESIGNED FOR THE GAME.</span>
        <span>© 2026</span>
      </footer>
    </div>
  );
}

export default App;
