import { createContext, useContext, useState, ReactNode } from "react";

interface CampaignContextType {
  campaignId: string | null;
  setCampaignId: (id: string) => void;
  isDm: boolean;
  setIsDm: (val: boolean) => void;
}

const CampaignContext = createContext<CampaignContextType>({
  campaignId: null,
  setCampaignId: () => {},
  isDm: false,
  setIsDm: () => {},
});

export function CampaignProvider({ children }: { children: ReactNode }) {
  // For the initial version, we hardcode a single campaign.
  // This can be extended to a campaign selector later.
  const [campaignId, setCampaignId] = useState<string | null>(
    localStorage.getItem("activeCampaignId")
  );
  const [isDm, setIsDm] = useState(false);

  const handleSetCampaign = (id: string) => {
    setCampaignId(id);
    localStorage.setItem("activeCampaignId", id);
  };

  return (
    <CampaignContext.Provider
      value={{ campaignId, setCampaignId: handleSetCampaign, isDm, setIsDm }}
    >
      {children}
    </CampaignContext.Provider>
  );
}

export const useCampaign = () => useContext(CampaignContext);
