import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { loginRequest } from "../../services/authConfig";
import { ReactNode } from "react";
import "./AppShell.css";

interface Props {
  children: ReactNode;
  isDm: boolean;
  activeTab: string;
  onTabChange: (tab: string) => void;
}

export function AppShell({ children, isDm, activeTab, onTabChange }: Props) {
  const { instance, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  const userName = accounts[0]?.name || accounts[0]?.username || "Adventurer";

  const handleLogin = () => instance.loginPopup(loginRequest);
  const handleLogout = () => instance.logoutPopup();

  const tabs = [
    { id: "characters", label: "Party" },
    { id: "wishlist", label: "Wishlists" },
    ...(isDm
      ? [
          { id: "magic-items", label: "Magic Items" },
          { id: "treasure-tables", label: "Treasure Tables" },
        ]
      : []),
  ];

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="header-content">
          <div className="header-brand">
            <div className="brand-icon">⚔</div>
            <div>
              <h1 className="brand-title">Arcane Ledger</h1>
              <span className="brand-subtitle">Campaign Manager</span>
            </div>
          </div>

          <nav className="header-nav">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                className={`nav-tab ${activeTab === tab.id ? "active" : ""}`}
                onClick={() => onTabChange(tab.id)}
              >
                {tab.label}
              </button>
            ))}
          </nav>

          <div className="header-user">
            {isAuthenticated ? (
              <>
                <span className="user-name">{userName}</span>
                {isDm && <span className="dm-badge">DM</span>}
                <button className="btn btn-sm" onClick={handleLogout}>
                  Sign Out
                </button>
              </>
            ) : (
              <button className="btn btn-primary btn-sm" onClick={handleLogin}>
                Sign In
              </button>
            )}
          </div>
        </div>
      </header>

      <main className="app-main">
        <div className="page-layout">{children}</div>
      </main>
    </div>
  );
}
