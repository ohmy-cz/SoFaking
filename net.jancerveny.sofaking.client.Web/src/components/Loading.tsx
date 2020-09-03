import React from "react";
import style from "./Loading.module.scss";

export default function Loading(): JSX.Element {
  return (
    <>
      <div className={style.loading}>
        <div>
          <div></div>
          <div></div>
          <div></div>
          <div></div>
        </div>
      </div>
    </>
  );
}
