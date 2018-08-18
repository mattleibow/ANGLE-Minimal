using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using GLenum = System.UInt32;
using GLshort = System.UInt16;
using GLint = System.Int32;
using GLuint = System.UInt32;
using GLsizei = System.Int32;
using GLfloat = System.Single;

namespace ManagedANGLE
{
    public class SimpleRenderer : IDisposable
    {
        private GLuint mProgram;
        private GLsizei mWindowWidth;
        private GLsizei mWindowHeight;

        private GLint mPositionAttribLocation;
        private GLuint mVertexPositionBuffer;

        private static GLfloat[] mVertexPositions = new[]
        {
             0.0f,  0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
             0.5f, -0.5f, 0.0f
        };

        private static bool UseBufferMethod = true;

        public SimpleRenderer()
        {
            mWindowWidth = 0;
            mWindowHeight = 0;

            string vs =
@"attribute vec4 aPosition;
void main() {
    gl_Position = aPosition;
}";

            string fs =
@"precision mediump float;
void main() {
    gl_FragColor = vec4(1, 0, 0, 1);
}";

            mProgram = CompileProgram(vs, fs);
            mPositionAttribLocation = Gles.glGetAttribLocation(mProgram, "aPosition");

            if (UseBufferMethod)
            {
                Gles.glGenBuffers(1, out mVertexPositionBuffer);
                Gles.glThrowError();

                Gles.glBindBuffer(Gles.GL_ARRAY_BUFFER, mVertexPositionBuffer);
                Gles.glThrowError();

                Gles.glBufferData(Gles.GL_ARRAY_BUFFER, mVertexPositions, Gles.GL_STATIC_DRAW);
                Gles.glThrowError();
            }
        }

        public void Dispose()
        {
            if (mProgram != 0)
            {
                Gles.glDeleteProgram(mProgram);
                mProgram = 0;
            }

            if (mVertexPositionBuffer != 0)
            {
                Gles.glDeleteBuffers(1, ref mVertexPositionBuffer);
                mVertexPositionBuffer = 0;
            }
        }

        public void Draw()
        {
            Gles.glEnable(Gles.GL_DEPTH_TEST);
            Gles.glClearColor(0, 0, 0.4f, 1);
            Gles.glClear(Gles.GL_COLOR_BUFFER_BIT | Gles.GL_DEPTH_BUFFER_BIT);

            if (mProgram == 0)
                return;

            Gles.glUseProgram(mProgram);
            Gles.glThrowError();

            if (UseBufferMethod)
            {
                Gles.glBindBuffer(Gles.GL_ARRAY_BUFFER, mVertexPositionBuffer);
                Gles.glThrowError();

                Gles.glVertexAttribPointer((GLuint)mPositionAttribLocation, 3, Gles.GL_FLOAT, Gles.GL_FALSE, 0, IntPtr.Zero);
                Gles.glThrowError();
            }
            else
            {
                Gles.glVertexAttribPointer((GLuint)mPositionAttribLocation, 3, Gles.GL_FLOAT, Gles.GL_FALSE, 0, mVertexPositions);
                Gles.glThrowError();
            }

            Gles.glEnableVertexAttribArray((GLuint)mPositionAttribLocation);
            Gles.glThrowError();

            Gles.glDrawArrays(Gles.GL_TRIANGLES, 0, 3);
            Gles.glThrowError();
        }

        public void UpdateWindowSize(GLsizei width, GLsizei height)
        {
            Gles.glViewport(0, 0, width, height);

            mWindowWidth = width;
            mWindowHeight = height;
        }

        private static GLuint CompileShader(GLenum type, string source)
        {
            GLuint shader = Gles.glCreateShader(type);

            Gles.glShaderSource(shader, 1, new[] { source }, new[] { source.Length });
            Gles.glCompileShader(shader);

            Gles.glGetShaderiv(shader, Gles.GL_COMPILE_STATUS, out GLint compileResult);

            if (compileResult == 0)
            {
                Gles.glGetShaderiv(shader, Gles.GL_INFO_LOG_LENGTH, out GLint infoLogLength);

                StringBuilder infoLog = new StringBuilder(infoLogLength);
                Gles.glGetShaderInfoLog(shader, (GLsizei)infoLog.Capacity, out GLsizei length, infoLog);

                string errorMessage = "Shader compilation failed: " + infoLog;
                throw new Exception(errorMessage);
            }

            return shader;
        }

        private static GLuint CompileProgram(string vsSource, string fsSource)
        {
            GLuint program = Gles.glCreateProgram();

            if (program == 0)
            {
                throw new Exception("Program creation failed");
            }

            GLuint vs = CompileShader(Gles.GL_VERTEX_SHADER, vsSource);
            GLuint fs = CompileShader(Gles.GL_FRAGMENT_SHADER, fsSource);

            if (vs == 0 || fs == 0)
            {
                Gles.glDeleteShader(fs);
                Gles.glDeleteShader(vs);
                Gles.glDeleteProgram(program);
                return 0;
            }

            Gles.glAttachShader(program, vs);
            Gles.glDeleteShader(vs);

            Gles.glAttachShader(program, fs);
            Gles.glDeleteShader(fs);

            Gles.glLinkProgram(program);

            Gles.glGetProgramiv(program, Gles.GL_LINK_STATUS, out GLint linkStatus);

            if (linkStatus == 0)
            {
                Gles.glGetProgramiv(program, Gles.GL_INFO_LOG_LENGTH, out GLint infoLogLength);

                StringBuilder infoLog = new StringBuilder(infoLogLength);
                Gles.glGetProgramInfoLog(program, (GLsizei)infoLog.Capacity, out GLsizei length, infoLog);

                string errorMessage = "Program link failed: " + infoLog;
                throw new Exception(errorMessage);
            }

            return program;
        }
    }
}
