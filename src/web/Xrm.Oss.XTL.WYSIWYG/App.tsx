import * as React from "react";

export interface AppState {
  user: UserInfo;
}

export default class App extends React.PureComponent<any, AppState> {
  constructor(props: any) {
    super(props);

    this.state = {
      user: undefined
    };

    this.setUser = this.setUser.bind(this);
    this.triggerUserReload = this.triggerUserReload.bind(this);
  }

  componentDidMount() {
    this.setUser();
  }

  setUser() {
    fetch("/login",
    {
      method: "POST",
      headers: [
        ["Content-Type", "application/json"]
      ],
      credentials: "include",
      body: JSON.stringify({ })
    })
    .then(results => {
      return results.json();
    })
    .then((result: ValidationResult) => {
      if (result.success) {
          this.setState({ user: result.userInfo });
      }
      else {
          this.setState({ user: undefined });
      }
    });
  }

  shouldComponentUpdate(nextProps: any) {
    return true;
  }

  triggerUserReload() {
    this.setUser();
  }

  render() {
    return (
      <div>
        <Header user={ this.state.user } triggerUserReload={ this.triggerUserReload } />
        <Main user={ this.state.user } triggerUserReload={ this.triggerUserReload }/>
      </div>
    );
  }
}
