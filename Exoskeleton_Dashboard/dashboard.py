import streamlit as st
import json
import plotly.graph_objects as pltobj
import pandas as pd
import numpy as np

st.set_page_config(page_title="Thesis VR Data Visualizer", layout="wide")
st.title("Hand Kinematics")

def load_json(file):
    """ Loads and parses the uploaded JSON File"""
    return json.load(file)

def compute_velocity_profile(trajectory_data):
    """Calculates instantaneous velocity from discrete 3D spatial samples"""
    df = pd.DataFrame(trajectory_data)
    if df.empty or len(df) < 2:
        return pd.DataFrame()
    df['dt'] = df['t_ms'].diff() / 1000.0  # seconds
    df['dx'] = df['x'].diff()
    df['dy'] = df['y'].diff()
    df['dz'] = df['z'].diff()
    
    # Euclidean distance between consecutive points
    df['dist'] = np.sqrt(df['dx']**2 + df['dy']**2 + df['dz']**2)
    
    # Instantaneous speed (m/s)
    df['velocity'] = df['dist'] / df['dt']
    df['velocity'] = df['velocity'].replace([np.inf, -np.inf], 0.0).fillna(0.0)
    return df

def plot_animated_3d_view(trajectory_data, spawn_pos=None, target_pos=None, title="Animated 3D Path"):
    if not trajectory_data:
        return pltobj.Figure()

    df = pd.DataFrame(trajectory_data)
    fig = pltobj.Figure()

    # Trace 0: Spawn Box
    if spawn_pos and isinstance(spawn_pos, dict):
        fig.add_trace(pltobj.Scatter3d(
            x=[float(spawn_pos.get('x', 0))], y=[float(spawn_pos.get('z', 0))], z=[float(spawn_pos.get('y', 0))],
            mode='markers', marker=dict(symbol='square', size=8, color='orange', line=dict(color='black', width=2)),
            name='Object Spawn'
        ))
    else:
        fig.add_trace(pltobj.Scatter3d(x=[None], y=[None], z=[None])) # Dummy trace to maintain index

    # Trace 1: Target Zone
    if target_pos and isinstance(target_pos, dict):
        # Set this to exactly HALF of your Unity Box Collider scale
        # Base Transform Position
        base_x = float(target_pos.get('x', 0))
        base_z = float(target_pos.get('z', 0)) # Mapped to Plotly Y
        base_y = float(target_pos.get('y', 0)) # Mapped to Plotly Z

        # Dimensions based on Unity Inspector (Half-extents for bounding box math)
        ext_x = 0.454 / 2.0  # Unity X scale
        ext_z = 0.245 / 2.0  # Unity Z scale (Plotly Y)
        ext_y = 0.258 / 2.0  # Unity Y scale * collider size (Plotly Z)
        
        # Center of the collider (offsetting Y based on Inspector data)
        c_x = base_x
        c_y = base_z 
        c_z = base_y + 0.094 

        # Define the 8 corners of the 3D box
        x_verts = [c_x - ext_x, c_x + ext_x, c_x + ext_x, c_x - ext_x, 
                   c_x - ext_x, c_x + ext_x, c_x + ext_x, c_x - ext_x]
        
        y_verts = [c_y - ext_z, c_y - ext_z, c_y + ext_z, c_y + ext_z, 
                   c_y - ext_z, c_y - ext_z, c_y + ext_z, c_y + ext_z]
                   
        z_verts = [c_z - ext_y, c_z - ext_y, c_z - ext_y, c_z - ext_y, 
                   c_z + ext_y, c_z + ext_y, c_z + ext_y, c_z + ext_y]

        fig.add_trace(pltobj.Mesh3d(
            x=x_verts,
            y=y_verts,
            z=z_verts,
            alphahull=0, # Automatically generates a convex hull (a box) around the points
            color='royalblue',
            opacity=0.2, # Semi-transparent
            name='Target Zone Volume'
        ))
    else:
        fig.add_trace(pltobj.Scatter3d(x=[None], y=[None], z=[None])) # Dummy trace to maintain index

    # Trace 2: The static faded path
    if not df.empty:
        fig.add_trace(pltobj.Scatter3d(
            x=df['x'], y=df['z'], z=df['y'],
            mode='lines',
            line=dict(width=3, color='lightgray'), 
            name='Trajectory Path',
            hoverinfo='skip'
        ))
    
    # Trace 3: The Animated Hand Marker (Starts at index 0)
    fig.add_trace(pltobj.Scatter3d(
        x=[df['x'].iloc[0]], y=[df['z'].iloc[0]], z=[df['y'].iloc[0]],
        mode='markers',
        marker=dict(size=10, color='firebrick', symbol='circle'),
        name='Hand Position'
    ))

    # Build the Animation Frames
    frames = []
    for index, row in df.iterrows():
        frames.append(pltobj.Frame(
            data=[pltobj.Scatter3d(
                x=[row['x']], y=[row['z']], z=[row['y']]
            )],
            name=str(row['t_ms']),
            traces=[3] 
        ))
    fig.frames = frames
    sliders = [dict(
        active=0,
        yanchor="top",
        xanchor="left",
        currentvalue=dict(font=dict(size=14), prefix="Time (ms): ", visible=True, xanchor="right"),
        transition=dict(duration=0),
        pad=dict(b=10, t=50),
        x=0.1, y=0,
        steps=[dict(args=[[f.name], dict(frame=dict(duration=0, redraw=True), mode="immediate", transition=dict(duration=0))],
                    label=f.name, method="animate") for f in frames]
    )]

    # Add Buttons, Slider, and Layout
    fig.update_layout(
        title=title,
        uirevision='constant',  
        scene=dict(xaxis_title='X', yaxis_title='Z', zaxis_title='Y', aspectmode='data', uirevision='constant'),
        margin=dict(l=0, r=0, b=0, t=40),
        legend=dict(yanchor="top", y=0.99, xanchor="left", x=0.01),
        updatemenus=[dict(
            type="buttons", showactive=False,
            y=-0.1, x=0.05, xanchor="left", yanchor="top", direction="left",
            buttons=[
                dict(label="▶ Play", method="animate", args=[None, dict(frame=dict(duration=50, redraw=True), transition=dict(duration=0), fromcurrent=True, mode="immediate")]),
                dict(label="⏸ Pause", method="animate", args=[[None], dict(frame=dict(duration=0, redraw=False), mode="immediate")])
            ]
        )],
        sliders=sliders
    )
    return fig

def plot_top_view(trajectory_data, spawn_pos=None, target_pos=None, title="Top Down View", rotate=False):
    if not trajectory_data:
        return pltobj.Figure()

    df = pd.DataFrame(trajectory_data)

    # Unity Coordinates- X left/right, Z forward/backward, and Y is up/down
    fig = pltobj.Figure()
    
    #  Draw Spawn Position
    if spawn_pos and isinstance(spawn_pos, dict):
        s_x = float(spawn_pos.get('x', 0))
        s_y = float(spawn_pos.get('z', 0))
        
        fig.add_trace(pltobj.Scatter(
            x=[s_x], y=[s_y],
            mode='markers',
            marker=dict(symbol='square', size=20, color='orange', line=dict(color='black', width=2)),
            name='Object Spawn'
        ))

    # Draw Target Drop Zone
    if target_pos and isinstance(target_pos, dict):
        box_size = 0.10 
        t_x = float(target_pos.get('x', 0))
        t_z = float(target_pos.get('z', 0))

        # Calculate 4 mathematical corners of the box
        box_x = [t_x - box_size, t_x + box_size, t_x + box_size, t_x - box_size, t_x - box_size]
        box_z = [t_z - box_size, t_z - box_size, t_z + box_size, t_z + box_size, t_z - box_size]

        fig.add_trace(pltobj.Scatter(
            x=box_x, y=box_z,
            fill="toself",
            fillcolor="rgba(65, 105, 225, 0.3)",
            line=dict(color="royalblue", width=2),
            mode='lines',
            name='Target Zone'
        ))

    # Draw Trajectory Path
    fig.add_trace(pltobj.Scatter(
        x=df['x'], y=df['z'], 
        mode='lines+markers',
        marker=dict(
            size=5, color=df['t_ms'], colorscale='Viridis', 
            showscale=True, colorbar=dict(title="Time (ms)", x=1.02)
        ),
        line=dict(width=2.5, color='lightgray'),
        name='Hand Path'
    ))

    fig.update_layout(
        title=title,
        xaxis_title='Lateral Axis - X (m)',
        yaxis_title='Depth Axis - Z (m)',
        yaxis=dict(scaleanchor="x", scaleratio=1, showgrid=True, gridcolor='#E5E5E5'),
        xaxis=dict(showgrid=True, gridcolor='#E5E5E5'),
        plot_bgcolor='white',
        margin=dict(l=0, r=0, b=0, t=40),
        legend=dict(yanchor="top", y=0.99, xanchor="left", x=0.01, bgcolor="rgba(255, 255, 255, 0.8)")
    )
    return fig

def plot_velocity_curve(vel_df):
    """Plots speed over time to evaluate trajectory smoothness"""
    fig = pltobj.Figure()
    fig.add_trace(pltobj.Scatter(
        x=vel_df['t_ms'] /1000.0,
        y=vel_df['velocity'],
        mode='lines',
        line=dict(color='firebrick', width=2),
        fill='tozeroy',
        fillcolor='rgba(178, 34, 34, 0.1)',
        name='Instantaneous Velocity'
    ))
    fig.update_layout(
        title="Velocity Profile (Smoothness Analysis)",
        xaxis_title="Elapsed Time (s)",
        yaxis_title="Hand Speed (m/s)",
        plot_bgcolor='white',
        xaxis=dict(showgrid=True, gridcolor='#E5E5E5'),
        yaxis=dict(showgrid=True, gridcolor='#E5E5E5'),
        margin=dict(l=0, r=0, b=0, t=40)
    )
    return fig


def plot_3d_view(trajectory_data, spawn_pos=None,env_data = None, target_pos=None, title="3D Spatial Path"):
    if not trajectory_data:
        return pltobj.Figure()

    df = pd.DataFrame(trajectory_data)
    fig = pltobj.Figure()

    # Note: Plotly standard 3D puts Z as Up. Unity puts Y as Up.
    # We map Unity Y -> Plotly Z, and Unity Z -> Plotly Y so the rotation feels natural.
    
    if spawn_pos and isinstance(spawn_pos, dict):
        fig.add_trace(pltobj.Scatter3d(
            x=[float(spawn_pos.get('x', 0))], y=[float(spawn_pos.get('z', 0))], z=[float(spawn_pos.get('y', 0))],
            mode='markers', marker=dict(symbol='square', size=8, color='orange', line=dict(color='black', width=2)),
            name='Object Spawn'
        ))

    if env_data and 'target_center' in env_data and 'target_size' in env_data:
        t_center = env_data['target_center']
        t_size = env_data['target_size']
        
        c_x = float(t_center.get('x', 0))
        c_y = float(t_center.get('z', 0)) # Unity Z maps to Plotly Y
        c_z = float(t_center.get('y', 0)) # Unity Y maps to Plotly Z

        # Extents (Half the size)
        ext_x = float(t_size.get('x', 0.454)) / 2.0
        ext_z = float(t_size.get('z', 0.245)) / 2.0 
        ext_y = float(t_size.get('y', 0.258)) / 2.0 

        # 8 Exact Corners
        x_verts = [c_x - ext_x, c_x + ext_x, c_x + ext_x, c_x - ext_x, 
                   c_x - ext_x, c_x + ext_x, c_x + ext_x, c_x - ext_x]
        y_verts = [c_y - ext_z, c_y - ext_z, c_y + ext_z, c_y + ext_z, 
                   c_y - ext_z, c_y - ext_z, c_y + ext_z, c_y + ext_z]
        z_verts = [c_z - ext_y, c_z - ext_y, c_z - ext_y, c_z - ext_y, 
                   c_z + ext_y, c_z + ext_y, c_z + ext_y, c_z + ext_y]

        # Explicitly define the 12 triangles that make up the 6 faces of the box
        # This prevents Plotly from warping the shape
        i_faces = [0, 0, 4, 4, 0, 0, 3, 3, 0, 0, 1, 1]
        j_faces = [1, 2, 5, 6, 1, 5, 2, 6, 3, 7, 2, 6]
        k_faces = [2, 3, 6, 7, 5, 4, 6, 7, 7, 4, 6, 5]

        fig.add_trace(pltobj.Mesh3d(
            x=x_verts, y=y_verts, z=z_verts,
            i=i_faces, j=j_faces, k=k_faces, # Explicit face mapping
            color='royalblue', 
            opacity=0.25, 
            name='Target Zone Volume'
        ))

    if not df.empty and 'x' in df.columns and 'y' in df.columns and 'z' in df.columns:
        fig.add_trace(pltobj.Scatter3d(
            x=df['x'], y=df['z'], z=df['y'],
            mode='lines+markers',
            marker=dict(
                size=5, 
                color=df.get('t_ms', 0), 
                colorscale='Turbo', # Brighter rainbow scale
                showscale=True, 
                colorbar=dict(title="Time (ms)", x=0.85)
            ),
            line=dict(width=3, color='rgba(200, 200, 200, 0.5)'), # Lighter, semi-transparent base line
            name='Hand Path'
        ))

    fig.update_layout(
        title=title,
        scene=dict(
            xaxis_title='X (Left/Right)', yaxis_title='Z (Forward/Back)', zaxis_title='Y (Height/Up)',
            aspectmode='data',
            # Force white background walls inside the 3D plot to make colors pop
            xaxis=dict(backgroundcolor="white", gridcolor="lightgray", showbackground=True, zerolinecolor="black"),
            yaxis=dict(backgroundcolor="white", gridcolor="lightgray", showbackground=True, zerolinecolor="black"),
            zaxis=dict(backgroundcolor="white", gridcolor="lightgray", showbackground=True, zerolinecolor="black")
        ),
        margin=dict(l=0, r=0, b=0, t=40),
        legend=dict(yanchor="top", y=0.99, xanchor="left", x=0.01),
        paper_bgcolor='rgba(0,0,0,0)', # Blends seamlessly with Streamlit dark/light mode
        plot_bgcolor='rgba(0,0,0,0)'
    )
    return fig

def plot_rotations(trajectory_data):
    if not trajectory_data:
        return pltobj.Figure()
        
    df = pd.DataFrame(trajectory_data)
    fig = pltobj.Figure()
    
    if not df.empty and 'pitch' in df.columns and 'yaw' in df.columns and 'roll' in df.columns:
        fig.add_trace(pltobj.Scatter(x=df['t_ms'], y=df['pitch'], mode='lines', name='Pitch (X-axis tilt)', line=dict(width=2)))
        fig.add_trace(pltobj.Scatter(x=df['t_ms'], y=df['yaw'], mode='lines', name='Yaw (Y-axis twist)', line=dict(width=2)))
        fig.add_trace(pltobj.Scatter(x=df['t_ms'], y=df['roll'], mode='lines', name='Roll (Z-axis lean)', line=dict(width=2)))
        
    fig.update_layout(
        title="Hand Rotation Over Time",
        xaxis_title="Time (ms)",
        yaxis_title="Euler Angles (°)",
        xaxis=dict(showgrid=True, gridcolor='lightgray', zeroline=True, zerolinecolor='black'),
        yaxis=dict(showgrid=True, gridcolor='lightgray', zeroline=True, zerolinecolor='black'),
        plot_bgcolor='white',
        hovermode="x unified"
    )
    return fig
    
# Sidebar to Load Files
with st.sidebar:
    st.header("Data Import")
    uploaded_file = st.file_uploader("Upload Session JSON", type=['json'])

if uploaded_file is not None:
    data = load_json(uploaded_file)
    session_id = data.get('session_id', 'Unknown')
    st.sidebar.success(f"Loaded: {session_id}")

    tab1,tab2 = st.tabs(["Session Overview" , "Repetition Kinematics"])
    
    with tab1:
        st.header("Overall Performance")
        metrics = data.get('session_metrics' , {})

        cols = st.columns(5)
        cols[0].metric("Total Reps", metrics.get('total_reps', 0))
        cols[1].metric("Successful Grabs", metrics.get('total_score', 0))
        cols[2].metric("Drops", metrics.get('total_drops', 0))
        cols[3].metric("Misses", metrics.get('total_misses', 0))
        cols[4].metric("Accuracy", f"{metrics.get('total_accuracy', 0):.1f}%")

        reps = data.get('repetitions' , [])
        if reps:
            st.divider()
            st.subheader("Trends (Motor Learning & Fatigue)")

            # Combine Repetion Metrics
            summary_list = []
            for r in reps:
                m = r.get('metrics', {})
                space = m.get('space_explored', 0.0)
                ideal = m.get('ideal_path_length', 0.0)
                efficiency = (ideal/space) if space > 0 else 0.0

                summary_list.append({
                    "Rep ID": r.get('rep_id'),
                    "Total Time (s)": m.get('total_time_ms', 0) / 1000.0,
                    "Reaction Time (s)": m.get('reaction_time_ms', 0) / 1000.0,
                    "Moving Time (s)": m.get('moving_time_ms', 0) / 1000.0,
                    "Space Explored (m)": space,
                    "Ideal Path Length (m)": ideal,
                    "Path Efficiency": efficiency,
                    "Peak Velocity (m/s)": m.get('peak_velocity', 0.0),
                    "Average FPS": m.get('average_fps', 0.0)
                })
            trends_df = pd.DataFrame(summary_list)
            t_col1, t_col2 = st.columns(2)
            with t_col1:
                # Time Trend
                fig_time = pltobj.Figure()
                fig_time.add_trace(pltobj.Scatter(
                    x=trends_df["Rep ID"], y=trends_df["Total Time (s)"],
                    mode='lines+markers', name="Total Time (s)", line=dict(color='royalblue')
                ))
                fig_time.add_trace(pltobj.Scatter(
                    x=trends_df["Rep ID"], y=trends_df["Moving Time (s)"],
                    mode='lines+markers', name="Moving Time (s)", line=dict(color='darkcyan', dash='dash')
                ))
                fig_time.update_layout(
                    title="Execution Time per Repetition",
                    xaxis_title="Repetition", yaxis_title="Time (s)",
                    plot_bgcolor='white', xaxis=dict(showgrid=True, gridcolor='#E5E5E5'),
                    yaxis=dict(showgrid=True, gridcolor='#E5E5E5')
                )
                st.plotly_chart(fig_time, width='stretch')

            with t_col2:
                # Path Efficiency Trend
                fig_eff = pltobj.Figure()
                fig_eff.add_trace(pltobj.Scatter(
                    x=trends_df["Rep ID"], y=trends_df["Path Efficiency"],
                    mode='lines+markers', name="Efficiency Index", line=dict(color='forestgreen', width=2)
                ))
                fig_eff.update_layout(
                    title="Path Efficiency Index (Ideal / Explored)",
                    xaxis_title="Repetition", yaxis_title="Efficiency Ratio (1.0 = Straight line)",
                    plot_bgcolor='white', xaxis=dict(showgrid=True, gridcolor='#E5E5E5'),
                    yaxis=dict(showgrid=True, gridcolor='#E5E5E5')
                )
                st.plotly_chart(fig_eff, width='stretch')

            # Tabular summary
            st.dataframe(trends_df.style.format({
                "Total Time (s)": "{:.2f}",
                "Reaction Time (s)": "{:.2f}",
                "Moving Time (s)": "{:.2f}",
                "Space Explored (m)": "{:.2f}",
                "Ideal Path Length (m)": "{:.2f}",
                "Path Efficiency": "{:.2f}",
                "Peak Velocity (m/s)": "{:.2f}",
                "Average FPS": "{:.1f}"
            }), width='stretch')

    with tab2:
        st.header("Kinematics")

        reps = data.get('repetitions' , [])
        if not reps:
            st.warning("No repetitions found in this session.")
        else:
            rep_options = [r['rep_id'] for r in reps]
            selected_rep = st.selectbox("Select Repetition to Analyze:", rep_options)

            rep_data = next(r for r in reps if r['rep_id'] == selected_rep)
            r_metrics = rep_data.get('metrics', {})
            trajectory = rep_data.get('trajectory' , [])

            c1,c2,c3,c4,c5 = st.columns(5)
            c1.metric("Total Time (ms)", f"{r_metrics.get('total_time_ms', 0):.0f}")
            c2.metric("Reaction Time (ms)", f"{r_metrics.get('reaction_time_ms', 0):.0f}")
            c3.metric("Space Explored ", f"{r_metrics.get('space_explored', 0):.2f}")
            c4.metric("Peak Velocity ", f"{r_metrics.get('peak_velocity', 0):.2f}")
            c5.metric("System FPS", f"{r_metrics.get('average_fps', 0):.1f}")

            st.divider()

            
            st.subheader("Top-Down Spatial Path")
            st.markdown("Displays movement across the horizontal plane. Color indicates elapsed time.")

            env_data = data.get('environment', {})
            
            # Extract and pass the spawn/target variables ---
            spawn_p = rep_data.get('spawn_position', None)
            target_p = rep_data.get('target_position', None)
           
            vel_df = compute_velocity_profile(trajectory)

            graph_tab1, graph_tab2, graph_tab3, graph_tab4 = st.tabs(["2D Top View", "3D Spatial View", "Rotation Analysis", "Velocity Profile"])

            with graph_tab1:
                fig2d = plot_top_view(trajectory, spawn_pos=spawn_p, target_pos=target_p, title=f"Repetition {selected_rep} - Top View")
                st.plotly_chart(fig2d, width='stretch')

            with graph_tab2:
                fig3d = plot_3d_view(trajectory, spawn_pos=spawn_p, env_data=env_data, target_pos=target_p, title=f"Repetition {selected_rep} - 3D View")
                st.plotly_chart(fig3d, width='stretch')
                
            with graph_tab3:
                figRot = plot_rotations(trajectory)
                st.plotly_chart(figRot, width='stretch')
                
            with graph_tab4:
                if not vel_df.empty:
                    figVel = plot_velocity_curve(vel_df)
                    st.plotly_chart(figVel, width='stretch')
                        
else:
    st.info("Please upload a Unity Session JSON file in the sidebar to begin analysis.")