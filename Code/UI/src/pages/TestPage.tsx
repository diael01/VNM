import { fetchInverterData } from "../api/inverterApi";
import { useState,useEffect } from "react";
import type { InverterData} from "../types/inverter";


export default function TestPageData() {
    const [loading,setLoading] = useState(true);
    const [data, setData] = useState<InverterData | null>(null);
    const [error,setError] = useState<string | null>(null);


    useEffect(() => {
       let mounted = true;
        async function loadData() {
                 setLoading(true);
                 setError(null);
            try {
                const result = await fetchInverterData();
                if(mounted)
                    setData(result);
            }
            catch(err)
            {
                 if(mounted)
                setError(err instanceof Error ? err.message : "Failed to load data");
            }
            finally {
                 if(mounted)
                setLoading(false);
            }                        
        }
        loadData();

        return () => {            
            mounted = false;
        }   
    },[])       

    if(loading)
        return <p>loading...</p>
        if(error)
        <p>error...</p>
    if(data === null)
        return <p>no data...</p>

    return (
        <div>
            <p>data.power</p>
        </div>
    )
}
