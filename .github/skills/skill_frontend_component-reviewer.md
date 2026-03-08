---
description: 'Skill especializada en revisión de componentes frontend. Verifica responsabilidad única, separación de lógica y UI, reutilización, tipado TypeScript y cumplimiento del design system definido en el SPEC.'
---

# Skill: component-reviewer [FRONTEND]

## Responsabilidad
Revisar y refactorizar componentes frontend para que cumplan con
los estándares de código limpio adaptados al frontend del lineamiento `dev-guidelines.md`.

---

## Checklist de Revisión de Componentes

### 1. Tamaño y Responsabilidad Única
- [ ] Ningún componente supera las 200 líneas
- [ ] Si supera → extraer subcomponentes con responsabilidad específica
- [ ] Cada componente hace UNA sola cosa

### 2. Separación Lógica/Presentación (Container/Presenter)
```tsx
// ❌ INCORRECTO — lógica y presentación mezcladas
const UserProfile = ({ userId }) => {
  const [user, setUser] = useState(null);
  useEffect(() => { fetch(`/api/users/${userId}`).then(...) }, [userId]);
  const handleUpdate = async (data) => { await fetch(...) };
  return <div>...</div>; // UI aquí mismo
};

// ✅ CORRECTO — separados
// Container (lógica)
const UserProfileContainer = ({ userId }) => {
  const { user, updateUser, isLoading } = useUserProfile(userId);
  return <UserProfileView user={user} onUpdate={updateUser} isLoading={isLoading} />;
};

// Presenter (solo UI)
const UserProfileView = ({ user, onUpdate, isLoading }) => {
  if (isLoading) return <Spinner />;
  return <div>...</div>;
};
```

### 3. Custom Hooks para Lógica Reutilizable
```tsx
// ❌ INCORRECTO — lógica repetida en varios componentes
const ComponentA = () => {
  const [data, setData] = useState([]);
  useEffect(() => { fetch('/api/items').then(r => r.json()).then(setData) }, []);
  ...
};

// ✅ CORRECTO — extraer a hook
const useItems = () => {
  const [data, setData] = useState<Item[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  useEffect(() => {
    itemsApi.getAll().then(setData).finally(() => setIsLoading(false));
  }, []);
  return { data, isLoading };
};
```

### 4. TypeScript y Tipado
- [ ] Todas las props tienen tipos explícitos con `interface` o `type`
- [ ] No usar `any` — buscar tipo correcto o usar `unknown` con validación
- [ ] Eventos tipados correctamente (`React.ChangeEvent<HTMLInputElement>`)
- [ ] Retorno de las funciones con tipo explícito

### 5. Manejo de Estados de UI
- [ ] Estados de loading manejados con componente visual
- [ ] Estados de error manejados con mensaje al usuario
- [ ] Estado vacío/empty manejado con componente visual
- [ ] No dejar pantallas en blanco sin feedback al usuario

### 6. Reutilización y Design System
- [ ] Usar los componentes del design system del proyecto
- [ ] No duplicar estilos — usar clases o tokens del design system
- [ ] Mantener consistencia visual con el resto de la aplicación

---

## Proceso de Revisión

```
PASO 1 → Listar todos los componentes del proyecto
PASO 2 → Identificar violaciones por categoría
PASO 3 → Reportar con ubicación exacta (archivo:línea)
PASO 4 → Aplicar refactoring: tamaño → separación → hooks → tipos
PASO 5 → Verificar que el render visual se mantiene igual
PASO 6 → Reportar cambios aplicados
```

## Reporte de Revisión

```
🎨 COMPONENT-REVIEWER [FRONTEND] — REPORTE
════════════════════════════════════════════════
Componentes analizados:                X
Violaciones encontradas:               X
  Componentes > 200 líneas:            X → refactorizados: X
  Lógica mezclada con UI:              X → separados: X
  Lógica duplicada sin hook:           X → extraída a hook: X
  Props sin tipar / uso de 'any':      X → tipados: X
  Estados de UI incompletos:           X → completados: X
  Violaciones design system:           X → corregidas: X
════════════════════════════════════════════════
```
