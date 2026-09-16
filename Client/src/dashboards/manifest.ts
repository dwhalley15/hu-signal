export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Hu Signal Dashboard",
    alias: "HuSignal.Dashboard",
    type: "dashboard",
    js: () => import("./dashboard.element.js"),
    meta: {
      label: "Hu Signal",
      pathname: "hu-signal",
    },
    conditions: [
      {
        alias: "Umb.Condition.SectionAlias",
        match: "Umb.Section.Content",
      },
    ],
  },
];