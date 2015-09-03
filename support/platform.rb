module Platform

  def self.nix?
    !RUBY_PLATFORM.match("linux|darwin").nil?
  end

  def self.runtime(cmd)
    command = cmd
    if self.nix?
      runtime = (CLR_TOOLS_VERSION || "v4.0.30319")
      command = "mono --runtime=#{runtime} #{cmd}"
    end
    command
  end

  def self.switch(arg)
    sw = self.nix? ? " -" : " /"
    sw + arg
  end

end
