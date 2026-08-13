import AddRoundedIcon from "@mui/icons-material/AddRounded";
import ArchiveRoundedIcon from "@mui/icons-material/ArchiveRounded";
import EditRoundedIcon from "@mui/icons-material/EditRounded";
import ToggleOffRoundedIcon from "@mui/icons-material/ToggleOffRounded";
import ToggleOnRoundedIcon from "@mui/icons-material/ToggleOnRounded";
import {
  Alert,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { ApiError } from "../../api/ApiError";
import { StatusChip } from "../../shared/components/StatusChip";
import { formatMoney } from "../../shared/format/money";
import { applicationRoles } from "../auth/types";
import { useAuthenticatedRequest } from "../auth/useAuthenticatedRequest";
import { useAuth } from "../auth/useAuth";
import {
  archiveResource,
  createResource,
  listResources,
  markResourceAvailable,
  markResourceUnavailable,
  updateResource,
} from "./resourcesApi";
import {
  resourceStatusLabels,
  resourceStatuses,
  resourceTypeLabels,
  resourceTypes,
} from "./resourceLabels";
import { ResourceFormDialog } from "./ResourceFormDialog";
import type {
  ResourceFormValues,
  ResourceItem,
  ResourceStatus,
  ResourceType,
} from "./types";

type ResourceTypeFilter = ResourceType | "All";
type ResourceStatusFilter = ResourceStatus | "All";

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : "Akcija trenutno nije uspela.";
}

function getResourceDetails(resource: ResourceItem) {
  if (resource.type === "Venue") {
    return resource.capacity === null
      ? "Kapacitet nije unet"
      : `${resource.capacity} mesta`;
  }

  if (resource.type === "Speaker") {
    return resource.expertiseArea ?? "Oblast ekspertize nije uneta";
  }

  return [
    resource.providerName,
    resource.supportedCapacity === null
      ? null
      : `${resource.supportedCapacity} mesta`,
    resource.serviceArea,
    resource.includesTechnicalSupport ? "tehnička podrška" : null,
  ]
    .filter(Boolean)
    .join(" · ");
}

function matchesSearch(resource: ResourceItem, searchTerm: string) {
  const normalizedSearch = searchTerm.trim().toLocaleLowerCase("sr-RS");

  if (!normalizedSearch) {
    return true;
  }

  return [
    resource.name,
    resource.description,
    resource.expertiseArea,
    resource.providerName,
    resource.serviceArea,
    resource.contentsSummary,
  ]
    .filter(Boolean)
    .some((value) =>
      value?.toLocaleLowerCase("sr-RS").includes(normalizedSearch),
    );
}

export function ResourcesPage() {
  const { session } = useAuth();
  const authenticatedRequest = useAuthenticatedRequest();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [resourceToEdit, setResourceToEdit] = useState<ResourceItem | null>(
    null,
  );
  const [formOpen, setFormOpen] = useState(false);
  const [typeFilter, setTypeFilter] = useState<ResourceTypeFilter>("All");
  const [statusFilter, setStatusFilter] = useState<ResourceStatusFilter>("All");
  const [searchTerm, setSearchTerm] = useState("");

  const isAdmin = Boolean(session?.user.roles.includes(applicationRoles.admin));

  const resourcesQueryKey = ["resources"];

  const { data: resources = [], isLoading } = useQuery({
    queryKey: resourcesQueryKey,
    queryFn: () => listResources(authenticatedRequest),
    enabled: Boolean(session?.accessToken),
  });

  const visibleResources = useMemo(
    () =>
      resources.filter(
        (resource) =>
          (typeFilter === "All" || resource.type === typeFilter) &&
          (statusFilter === "All" || resource.status === statusFilter) &&
          matchesSearch(resource, searchTerm),
      ),
    [resources, searchTerm, statusFilter, typeFilter],
  );

  const refreshResources = async () => {
    await queryClient.invalidateQueries({ queryKey: resourcesQueryKey });
  };

  const createMutation = useMutation({
    mutationFn: (values: ResourceFormValues) =>
      createResource(authenticatedRequest, values),
    onSuccess: async () => {
      setError(null);
      setFormOpen(false);
      await refreshResources();
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  });

  const updateMutation = useMutation({
    mutationFn: (values: ResourceFormValues) => {
      if (!resourceToEdit) {
        throw new Error("Resurs nije izabran.");
      }

      return updateResource(authenticatedRequest, resourceToEdit.id, values);
    },
    onSuccess: async () => {
      setError(null);
      setResourceToEdit(null);
      setFormOpen(false);
      await refreshResources();
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  });

  const actionMutation = useMutation({
    mutationFn: async ({
      resource,
      action,
    }: {
      resource: ResourceItem;
      action: "mark-available" | "mark-unavailable" | "archive";
    }) => {
      if (action === "mark-available") {
        await markResourceAvailable(authenticatedRequest, resource.id);
      } else if (action === "mark-unavailable") {
        await markResourceUnavailable(authenticatedRequest, resource.id);
      } else {
        await archiveResource(authenticatedRequest, resource.id);
      }
    },
    onSuccess: async () => {
      setError(null);
      await refreshResources();
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  });

  const isFormSubmitting = createMutation.isPending || updateMutation.isPending;

  const handleFormSubmit = (values: ResourceFormValues) => {
    if (resourceToEdit) {
      updateMutation.mutate(values);
      return;
    }

    createMutation.mutate(values);
  };

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={2}
        sx={{ justifyContent: "space-between" }}
      >
        <Stack spacing={0.75}>
          <Typography component="h1" variant="h4">
            Resursi
          </Typography>
          <Typography color="text.secondary">
            Pregled sala, predavača i paketa opreme za planiranje događaja.
          </Typography>
        </Stack>
        {isAdmin && (
          <Button
            variant="contained"
            startIcon={<AddRoundedIcon />}
            onClick={() => {
              setResourceToEdit(null);
              setFormOpen(true);
            }}
          >
            Novi resurs
          </Button>
        )}
      </Stack>

      {error && <Alert severity="error">{error}</Alert>}

      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={2}
        sx={{ alignItems: { md: "center" } }}
      >
        <TextField
          label="Pretraži"
          value={searchTerm}
          onChange={(event) => setSearchTerm(event.target.value)}
          sx={{ minWidth: { md: 320 } }}
        />
        <FormControl sx={{ minWidth: 190 }}>
          <InputLabel id="resource-type-filter-label">Tip</InputLabel>
          <Select
            labelId="resource-type-filter-label"
            label="Tip"
            value={typeFilter}
            onChange={(event) =>
              setTypeFilter(event.target.value as ResourceTypeFilter)
            }
          >
            <MenuItem value="All">Svi tipovi</MenuItem>
            {resourceTypes.map((type) => (
              <MenuItem key={type} value={type}>
                {resourceTypeLabels[type]}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl sx={{ minWidth: 190 }}>
          <InputLabel id="resource-status-filter-label">Status</InputLabel>
          <Select
            labelId="resource-status-filter-label"
            label="Status"
            value={statusFilter}
            onChange={(event) =>
              setStatusFilter(event.target.value as ResourceStatusFilter)
            }
          >
            <MenuItem value="All">Svi statusi</MenuItem>
            {resourceStatuses.map((status) => (
              <MenuItem key={status} value={status}>
                {resourceStatusLabels[status]}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Stack>

      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Resurs</TableCell>
              <TableCell>Tip</TableCell>
              <TableCell>Detalji</TableCell>
              <TableCell>Cena</TableCell>
              <TableCell>Kvalitet</TableCell>
              <TableCell>Status</TableCell>
              {isAdmin && <TableCell align="right">Akcije</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={isAdmin ? 7 : 6}>
                  Učitavanje resursa...
                </TableCell>
              </TableRow>
            )}
            {!isLoading && visibleResources.length === 0 && (
              <TableRow>
                <TableCell colSpan={isAdmin ? 7 : 6}>
                  Nema resursa za izabrane filtere.
                </TableCell>
              </TableRow>
            )}
            {visibleResources.map((resource) => (
              <TableRow key={resource.id} hover>
                <TableCell>
                  <Stack spacing={0.35}>
                    <Typography sx={{ fontWeight: 650 }}>
                      {resource.name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {resource.description}
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell>{resourceTypeLabels[resource.type]}</TableCell>
                <TableCell>{getResourceDetails(resource)}</TableCell>
                <TableCell>{formatMoney(resource.cost)}</TableCell>
                <TableCell>{resource.qualityScore}/5</TableCell>
                <TableCell>
                  <StatusChip status={resource.status} />
                </TableCell>
                {isAdmin && (
                  <TableCell align="right">
                    {resource.status === "Archived" ? (
                      <Typography variant="body2" color="text.secondary">
                        Arhiviran
                      </Typography>
                    ) : (
                      <Stack
                        direction="row"
                        spacing={1}
                        sx={{ justifyContent: "flex-end", flexWrap: "wrap" }}
                      >
                        <Button
                          size="small"
                          startIcon={<EditRoundedIcon />}
                          onClick={() => {
                            setResourceToEdit(resource);
                            setFormOpen(true);
                          }}
                        >
                          Izmeni
                        </Button>
                        {resource.status === "Available" ? (
                          <Button
                            size="small"
                            startIcon={<ToggleOffRoundedIcon />}
                            loading={actionMutation.isPending}
                            onClick={() =>
                              actionMutation.mutate({
                                resource,
                                action: "mark-unavailable",
                              })
                            }
                          >
                            Nedostupan
                          </Button>
                        ) : (
                          <Button
                            size="small"
                            startIcon={<ToggleOnRoundedIcon />}
                            loading={actionMutation.isPending}
                            onClick={() =>
                              actionMutation.mutate({
                                resource,
                                action: "mark-available",
                              })
                            }
                          >
                            Dostupan
                          </Button>
                        )}
                        <Button
                          size="small"
                          color="error"
                          startIcon={<ArchiveRoundedIcon />}
                          loading={actionMutation.isPending}
                          onClick={() =>
                            actionMutation.mutate({
                              resource,
                              action: "archive",
                            })
                          }
                        >
                          Arhiviraj
                        </Button>
                      </Stack>
                    )}
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <ResourceFormDialog
        open={formOpen}
        resource={resourceToEdit}
        isSubmitting={isFormSubmitting}
        onClose={() => {
          if (!isFormSubmitting) {
            setFormOpen(false);
            setResourceToEdit(null);
          }
        }}
        onSubmit={handleFormSubmit}
      />
    </Stack>
  );
}
