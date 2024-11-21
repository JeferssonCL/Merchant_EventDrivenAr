import React from "react";
import styles from "@/styles/Option.module.css";
import { useRouter } from "next/navigation";

interface FormInputProps {
  icon: React.ElementType;
  text: string;
  route?: string;
  completeRoute?: boolean;
  onRouteChange?: (route: string) => void;
  pushing? : boolean;
}

export const Option: React.FC<FormInputProps> = ({
  icon: Icon,
  text,
  route = "",
  completeRoute = false,
  onRouteChange = () => {},
  pushing = true
}) => {
  const router = useRouter();

  const changeRoute = (route : string) =>{
    if (pushing) {
      router.push(route);
    }else{
      router.replace(route);
    }
  }

  const handleRouterNavigation = () => {
    const formattedText = text.toLowerCase().replace(/\s/g, "-");
    if (completeRoute) {
      onRouteChange(`/${route}`);
      changeRoute(`/${route}`);
      return;
    }
    if (formattedText !== "home") {
      if (route) {
        changeRoute(`/${route}/${formattedText}`);
        onRouteChange(`/${route}/${formattedText}`);
      } else {
        changeRoute(`/${formattedText}`);
      }
    } else {
      router.push("/");
    }
  };
  return (
    <div className={styles.container} onClick={handleRouterNavigation}>
      <Icon className={styles.icon} />
      <p>{text}</p>
    </div>
  );
};
