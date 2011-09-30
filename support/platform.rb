module Platform

  def self.is_nix
    !RUBY_PLATFORM.match("linux|darwin").nil?
  end

  def self.runtime(cmd)
    command = cmd
    if self.is_nix
      runtime = (CLR_TOOLS_VERSION || "v4.0.30319")
      command = "mono --runtime=#{runtime} #{cmd}"
    end
    command
  end

  def self.switch(arg)
    sw = self.is_nix ? " -" : " /"
    sw + arg
  end

end
