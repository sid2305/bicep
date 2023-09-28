import { useEffect, useState } from "react";
import ReactLogo from "./assets/react.svg?react";
// import ViteLogo from "/vite.svg";
import "./App.css";


function App() {
  const [count, setCount] = useState(0);

  useEffect(() => {
    const loadAll = async () => {
      const test = (await import("./assets/react.svg?react")).default;


      // const components = import.meta.glob<React.FC<React.SVGProps<SVGSVGElement>>>(
      //   "./assets/*.svg",
      //   {
      //     eager: true,
      //     import: "default",
      //     query: "react",
      //   },
      // );

      console.log(test);
    };

    loadAll();
  }, []);

  return (
    <>
      <ReactLogo />
      <h1>Vite + React</h1>
      <div className="card">
        <button onClick={() => setCount((count) => count + 1)}>
          count is {count}
        </button>
        <p>
          Edit2 <code>src/App.tsx</code> and save to test HMR
        </p>
      </div>
      <p className="read-the-docs">
        Click on the Vite and React logos to learn more
      </p>
    </>
  );
}

export default App;
